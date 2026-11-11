using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BudsConnect;
using BudsConnect.Core;
using BudsConnect.Services;
using BudsConnect.ViewModels;

internal static class Program
{
    private static int count;
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Check(BudsProtocol.Crc(Encoding.ASCII.GetBytes("123456789")) == 0x31C3, "CRC-16/XMODEM independent check vector");
            // Captured legacy message body in protocol reference; same CRC convention for Buds3.
            byte[] body = Convert.FromHexString("6102004B5F01000000010500020013");
            Check(BudsProtocol.Crc(body) == 0xF30F, "Published Samsung status CRC fixture");
            byte[] payload = { 1, 86, 82, 1, 0, 0x11, 64, 0 };
            byte[] frame = BudsProtocol.Encode(0x60, payload);
            for (int split = 0; split <= frame.Length; split++)
            {
                var parser = new BudsProtocol();
                var found = parser.Feed(frame.AsSpan(0, split)).Concat(parser.Feed(frame.AsSpan(split))).ToArray();
                Check(found.Length == 1 && found[0].Payload.SequenceEqual(payload), $"Stream split at {split}");
            }
            var byteParser = new BudsProtocol();
            var bytePackets = frame.SelectMany(b => byteParser.Feed(new[] { b })).ToArray();
            Check(bytePackets.Length == 1, "Byte-at-a-time transport");
            Check(new BudsProtocol().Feed(frame.Concat(frame).ToArray()).Count == 2, "Coalesced frames");
            var corrupt = (byte[])frame.Clone(); corrupt[4] ^= 1;
            Check(new BudsProtocol().Feed(corrupt.Concat(frame).ToArray()).Count == 1, "Corrupt CRC rejected and stream recovered");
            Check(new BudsProtocol().Feed(new byte[] { 0, 0xFD, 0, 0, 77 }.Concat(frame).ToArray()).Count == 1, "Noise and invalid size recovery");
            var fragment = (byte[])frame.Clone(); fragment[2] |= 0x20;
            Check(new BudsProtocol().Feed(fragment).Count == 0, "Fragmented transfers never decoded as status");
            Check(new BudsProtocol().Feed(BudsProtocol.Encode(0x60, payload, true))[0].Response, "Response flag preserved");
            var now = DateTimeOffset.Now;
            var state = BudsProtocol.Apply(new(true), new(0x60, payload, false), now);
            Check(state.Left == 86 && state.Right == 82 && state.Case == 64 && state.TrayBattery == 82, "Buds3 short status and tray minimum");
            var extended = new byte[13]; extended[0] = 1; extended[2] = 90; extended[3] = 72; extended[7] = 55; extended[12] = 1;
            state = BudsProtocol.Apply(state, new(0x61, extended, false), now);
            Check(state.Left == 90 && state.Right == 72 && state.Case == 55 && state.Anc == true, "Buds3 extended status offsets");
            Check(BudsProtocol.Apply(state, new(0x60, new byte[2], false), now) == state, "Truncated status cannot overwrite state");
            Check(BudsProtocol.Apply(state, new(0x77, new byte[] { 0 }, false), now).Anc == false, "External ANC change");
            Check(BudsProtocol.Apply(state, new(0x77, new byte[] { 3 }, false), now).Anc == null, "Unknown mode not misrepresented as ANC off");
            state = BudsProtocol.Apply(state, new(0x60, new byte[] { 1, 255, 0, 1, 0, 0, 255 }, false), now);
            Check(state.Left == null && state.Right == 0 && state.Case == null && state.TrayBattery == 0, "Unavailable values distinct from real zero battery");
            Check((state with { Connected = false }).TrayBattery == null, "No stale tray percentage after disconnect");
            Check(BluetoothBudsService.IsBuds3("Galaxy Buds3 (Tomáš)") && BluetoothBudsService.IsBuds3("Galaxy Buds 3"), "Buds3 name recognition");
            Check(!BluetoothBudsService.IsBuds3("Galaxy Buds3 Pro") && !BluetoothBudsService.IsBuds3("Galaxy Buds3 FE"), "Other models excluded");
            Check(BluetoothBudsService.DescribeError(new System.Runtime.InteropServices.COMException("busy", unchecked((int)0x80072740))).Contains("jiná aplikace"), "Busy Bluetooth socket has actionable error");
            if (args.Contains("--render")) Render();
            if (args.Contains("--probe")) Probe();
            Console.WriteLine($"PASS: {count} checks"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Check(bool condition, string description)
    { if (!condition) throw new Exception("FAIL: " + description); count++; }
    private static void Render()
    {
        var app = new App(); app.InitializeComponent();
        using var service = new BluetoothBudsService();
        var vm = new MainViewModel(service, new Preferences());
        var window = new MainWindow(vm);
        var settings = new SettingsWindow(vm);
        var tray = new TrayWindow(vm);
        Check(window.Content != null && settings.Content != null && tray.Content != null, "All WPF windows load");
        var root = (FrameworkElement)window.Content!;
        root.Measure(new Size(424, 611)); root.Arrange(new Rect(0, 0, 424, 611)); root.UpdateLayout();
        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        { dc.DrawRectangle((Brush)app.Resources["Surface"], null, new Rect(0, 0, 424, 611)); }
        var bitmap = new RenderTargetBitmap(636, 917, 144, 144, PixelFormats.Pbgra32); bitmap.Render(drawing); bitmap.Render(root);
        string path = Path.GetFullPath("TestResults/main-window.png"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); encoder.Save(file);
        Console.WriteLine("UI render: " + path); vm.Stop();
    }
    private static void Probe()
    {
        using var service = new BluetoothBudsService();
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
        timer.Tick += (_, _) => frame.Continue = false;
        service.Changed += state =>
        {
            Console.WriteLine($"Bluetooth: {state.Status} | L={state.Left} R={state.Right} Case={state.Case} ANC={state.Anc}");
            if (state.Updated.HasValue) frame.Continue = false;
        };
        timer.Start(); service.Start(); System.Windows.Threading.Dispatcher.PushFrame(frame); timer.Stop();
    }
}

