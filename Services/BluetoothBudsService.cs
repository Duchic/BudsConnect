using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BudsConnect.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BudsConnect.Services;

public sealed class BluetoothBudsService : IBudsService, IDisposable
{
    private static readonly Guid ServiceId = new("2e73a4ad-332d-41fc-90e2-16bef06523f2");
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim writes = new(1, 1);
    private readonly SemaphoreSlim retry = new(0, 1);
    private CancellationTokenSource? session;
    private StreamSocket? socket;
    private TaskCompletionSource<bool>? ancReply;
    private bool desiredAnc;
    private Task? worker;
    private BudsState state = new();
    public event Action<BudsState>? Changed;

    public void Start() => worker ??= RunAsync(lifetime.Token);
    public void Reconnect()
    {
        session?.Cancel();
        if (retry.CurrentCount == 0) retry.Release();
    }

    public async Task SetAncAsync(bool enabled)
    {
        if (!state.Connected || state.Anc is null || ancReply != null)
            throw new InvalidOperationException("Ovládání ANC nyní není dostupné.");
        var reply = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ancReply = reply;
        desiredAnc = enabled;
        try
        {
            await SendAsync(0x78, new byte[] { enabled ? (byte)1 : (byte)0 }, false, lifetime.Token);
            bool ok = await reply.Task.WaitAsync(TimeSpan.FromSeconds(5), lifetime.Token);
            if (!ok) throw new IOException("Sluchátka změnu ANC odmítla.");
        }
        catch (TimeoutException)
        {
            throw new IOException("Sluchátka nepotvrdila změnu ANC. Zkus to znovu.");
        }
        finally { ancReply = null; }
    }

    private async Task RunAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stop);
            session = attempt;
            try
            {
                Publish(new(Status: "Hledám spárovaná Galaxy Buds3…"));
                var devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)).AsTask(attempt.Token);
                var candidates = devices.Where(d => IsBuds3(d.Name)).ToArray();
                if (candidates.Length == 0)
                    throw new IOException("Galaxy Buds3 nejsou spárovaná. Přidej je v nastavení Bluetooth Windows.");
                if (candidates.Length > 1)
                    throw new IOException("Nalezeno více Galaxy Buds3. Pro první připojení ponech spárovaný jen jeden pár.");
                using var device = await BluetoothDevice.FromIdAsync(candidates[0].Id).AsTask(attempt.Token);
                if (device == null) throw new IOException("Windows nepovolil přístup ke sluchátkům.");
                Publish(new(Name: device.Name, Status: "Připojuji ke sluchátkům…"));
                using var discovery = CancellationTokenSource.CreateLinkedTokenSource(attempt.Token);
                discovery.CancelAfter(TimeSpan.FromSeconds(15));
                var services = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(ServiceId), BluetoothCacheMode.Uncached).AsTask(discovery.Token);
                if (services.Error != BluetoothError.Success || services.Services.Count == 0)
                    throw new IOException("Sluchátka nejsou dostupná. Otevři pouzdro a ukonči jiné aplikace pro Galaxy Buds.");
                using var service = services.Services[0];
                using var connection = new StreamSocket();
                socket = connection;
                await connection.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName,
                    SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication).AsTask(discovery.Token);
                Publish(new(true, device.Name, Status: "Připojeno · čekám na údaje sluchátek"));
                device.ConnectionStatusChanged += OnConnectionStatus;
                try
                {
                    using var reader = new DataReader(connection.InputStream) { InputStreamOptions = InputStreamOptions.Partial };
                    var protocol = new BudsProtocol();
                    bool initialized = false;
                    while (!attempt.IsCancellationRequested)
                    {
                        using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(attempt.Token);
                        // Initial status must arrive; otherwise reconnect rather than show a false success.
                        if (!initialized) readTimeout.CancelAfter(TimeSpan.FromSeconds(12));
                        uint length = await reader.LoadAsync(1024).AsTask(readTimeout.Token);
                        if (length == 0) throw new IOException("Sluchátka ukončila spojení.");
                        byte[] bytes = new byte[length];
                        reader.ReadBytes(bytes);
                        foreach (var packet in protocol.Feed(bytes))
                        {
                            var next = BudsProtocol.Apply(state, packet, DateTimeOffset.Now);
                            if (next != state) Publish(next with { Status = "Připojeno · Bluetooth" });
                            if (!initialized && next.Updated.HasValue)
                            {
                                initialized = true;
                                // Mirror initial status as a response, then identify the manager.
                                await SendAsync(packet.Id, packet.Payload, true, attempt.Token);
                                await SendAsync(0x88, new byte[] { 1, 2, 34 }, false, attempt.Token);
                            }
                            if (ancReply != null)
                            {
                                if (BudsProtocol.AncConfirmation(packet, desiredAnc) is { } success)
                                {
                                    if (success && packet.Id == 0x51) Publish(state with { Anc = desiredAnc });
                                    ancReply.TrySetResult(success);
                                }
                            }
                        }
                    }
                }
                finally { device.ConnectionStatusChanged -= OnConnectionStatus; }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
            catch (OperationCanceledException) { Publish(new(Status: "Spojení přerušeno nebo vypršel čas. Zkusím se znovu připojit…")); }
            catch (Exception ex)
            {
                AppLog.Write(ex);
                Publish(new(Status: DescribeError(ex)));
            }
            finally
            {
                socket = null;
                ancReply?.TrySetException(new IOException("Spojení se sluchátky bylo přerušeno."));
                session = null;
            }
            try { await retry.WaitAsync(TimeSpan.FromSeconds(8), stop); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void OnConnectionStatus(BluetoothDevice device, object args)
    {
        if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected) session?.Cancel();
    }

    public static bool IsBuds3(string name)
    {
        string compact = name.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        return compact.Contains("buds3", StringComparison.Ordinal) && !compact.Contains("buds3pro", StringComparison.Ordinal)
            && !compact.Contains("buds3fe", StringComparison.Ordinal);
    }

    public static string DescribeError(Exception ex) => unchecked((uint)ex.HResult) switch
    {
        0x80072740 => "Spojení používá jiná aplikace. Ukonči Galaxy Buds / BudsManager a klikni na Znovu připojit.",
        0x80070005 => "Windows nepovolil přístup k Bluetooth. Zkontroluj oprávnění a nastavení Bluetooth.",
        _ => ex is IOException ? ex.Message : "Bluetooth není dostupné. Zkontroluj Bluetooth ve Windows a otevři pouzdro."
    };

    private async Task SendAsync(byte id, byte[] payload, bool response, CancellationToken token)
    {
        await writes.WaitAsync(token);
        try
        {
            var current = socket ?? throw new IOException("Sluchátka nejsou připojena.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var writer = new DataWriter(current.OutputStream);
            try
            {
                writer.WriteBytes(BudsProtocol.Encode(id, payload, response));
                await writer.StoreAsync().AsTask(timeout.Token);
                await writer.FlushAsync().AsTask(timeout.Token);
            }
            finally { writer.DetachStream(); }
        }
        finally { writes.Release(); }
    }
    private void Publish(BudsState value) { state = value; Changed?.Invoke(value); }
    public void Dispose() { lifetime.Cancel(); socket?.Dispose(); }
}
