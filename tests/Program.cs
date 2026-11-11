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
            var ackOn = new BudsPacket(0x42, new byte[] { 0x78, 1 }, true);
            var ackOff = new BudsPacket(0x42, new byte[] { 0x78, 0 }, true);
            // Actual Buds3 frames captured during the failing ANC toggle (no identifiers).
            byte[] capturedOn = Convert.FromHexString("FD05904278011CE2DD");
            byte[] capturedOff = Convert.FromHexString("FD05D04278003DF2DD");
            foreach (byte[] captured in new[] { capturedOn, capturedOff })
                for (int split = 0; split <= captured.Length; split++)
                {
                    var parser = new BudsProtocol();
                    var decoded = parser.Feed(captured.AsSpan(0, split)).Concat(parser.Feed(captured.AsSpan(split))).ToArray();
                    bool desired = captured == capturedOn;
                    Check(decoded.Length == 1 && BudsProtocol.AncConfirmation(decoded[0], desired) == true &&
                        BudsProtocol.Apply(state, decoded[0], now).Anc == desired,
                        $"Real Buds3 ACK header survives streaming split {split}, ANC={desired}");
                }
            for (int sequence = 0; sequence < 4; sequence++)
            {
                var sequenced = (byte[])capturedOn.Clone(); sequenced[2] = (byte)(0x10 | sequence << 6);
                Check(new BudsProtocol().Feed(sequenced).Count == 1, $"All high header bit combinations accepted: {sequence}");
            }
            Check(BudsProtocol.Apply(state, ackOn, now).Anc == true, "Universal ACK reports ANC on");
            Check(BudsProtocol.Apply(state, ackOff, now).Anc == false, "Universal ACK reports ANC off");
            Check(BudsProtocol.AncConfirmation(ackOn, true) == true, "Universal ACK mode one is not an error code");
            Check(BudsProtocol.AncConfirmation(ackOff, false) == true, "Universal ACK confirms off command");
            Check(BudsProtocol.AncConfirmation(ackOff, true) == false, "Mismatched applied mode is not success");
            foreach (var ignored in new[] { new BudsPacket(0x42, new byte[] { 0x78 }, true), new BudsPacket(0x42, new byte[] { 0x86, 1 }, true), new BudsPacket(0x42, new byte[] { 0x78, 255 }, true), new BudsPacket(0x61, new byte[8], true) })
                Check(BudsProtocol.AncConfirmation(ignored, true) == null, "Unrelated, malformed or unknown ACK does not confirm ANC");
            Check(BudsProtocol.AncConfirmation(new(0x51, new byte[] { 0x78, 0 }, true), true) == true, "Legacy success response remains supported");
            Check(BudsProtocol.AncConfirmation(new(0x51, new byte[] { 0x78, 1 }, true), true) == false, "Legacy error response remains an error");
            state = BudsProtocol.Apply(state, new(0x60, new byte[] { 1, 255, 0, 1, 0, 0, 255 }, false), now);
            Check(state.Left == null && state.Right == 0 && state.Case == null && state.TrayBattery == 0, "Unavailable values distinct from real zero battery");
            Check((state with { Connected = false }).TrayBattery == null, "No stale tray percentage after disconnect");
            Check(BluetoothBudsService.IsBuds3("Galaxy Buds3 (Tomáš)") && BluetoothBudsService.IsBuds3("Galaxy Buds 3"), "Buds3 name recognition");
            Check(!BluetoothBudsService.IsBuds3("Galaxy Buds3 Pro") && !BluetoothBudsService.IsBuds3("Galaxy Buds3 FE"), "Other models excluded");
            Check(BluetoothBudsService.DescribeError(new System.Runtime.InteropServices.COMException("busy", unchecked((int)0x80072740))).Contains("jiná aplikace"), "Busy Bluetooth socket has actionable error");
            if (args.Contains("--render")) Render();
            if (args.Contains("--probe")) Probe();
            if (args.Contains("--diagnose-anc")) DiagnoseAnc();
            Console.WriteLine($"PASS: {count} checks"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Check(bool condition, string description)
    { if (!condition) throw new Exception("FAIL: " + description); count++; }
    private static void Render()
    {
        var app = new TestApp();
        app.Resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/BudsConnect;component/ThemeResources.xaml") };
        using var service = new BluetoothBudsService();
        var vm = new MainViewModel(service, new Preferences());
        var window = new MainWindow(vm);
        var settings = new SettingsWindow(vm);
        var tray = new TrayWindow(vm);
        Check(window.Content != null && settings.Content != null && tray.Content != null, "All WPF windows load");
        foreach (bool dark in new[] { false, true, false })
        {
            ThemePalette.Apply(app.Resources, dark);
            foreach (var view in new Window[] { window, settings, tray })
            {
                var expected = ((SolidColorBrush)app.Resources["Surface"]).Color;
                Check(view.Background is SolidColorBrush surface && surface.Color == expected,
                    $"{view.GetType().Name} uses the {(dark ? "dark" : "light")} application background");
                Check(Contrast(((SolidColorBrush)view.Foreground).Color, expected) >= 4.5,
                    $"{view.GetType().Name} text contrasts against its actual background");
                RenderWindow(view, view == window ? 424 : view == settings ? 404 : 308,
                    view == window ? 611 : view == settings ? 495 : 320,
                    $"{view.GetType().Name}-{(dark ? "dark" : "light")}");
            }
            foreach (string text in new[] { "Text", "Muted", "Accent" })
                foreach (string surface in new[] { "Surface", "Soft", "Selected" })
                    Check(Contrast(((SolidColorBrush)app.Resources[text]).Color, ((SolidColorBrush)app.Resources[surface]).Color) >= 4.5,
                        $"{text} on {surface} has readable contrast (dark={dark})");
            var combo = (System.Windows.Controls.ComboBox)settings.FindName("ThemeBox");
            Check(Contrast(((SolidColorBrush)combo.Foreground).Color, ((SolidColorBrush)combo.Background).Color) >= 4.5,
                "Native appearance picker has matching text and surface colors");
        }
        vm.Stop();
        TestAncUi(app);
    }
    private static void TestAncUi(App app)
    {
        Console.WriteLine("Testing ANC UI...");
        System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(app.Dispatcher));
        var device = new FakeBudsService();
        var vm = new MainViewModel(device, new Preferences());
        var main = new MainWindow(vm);
        var tray = new TrayWindow(vm);
        foreach (var window in new Window[] { main, tray })
        { var root = (FrameworkElement)window.Content; root.Measure(new Size(424, 650)); root.Arrange(new Rect(0, 0, 424, 650)); root.UpdateLayout(); }
        var mainButtons = Descendants<System.Windows.Controls.RadioButton>((DependencyObject)main.Content).ToArray();
        var trayButtons = Descendants<System.Windows.Controls.RadioButton>((DependencyObject)tray.Content).ToArray();
        Check(mainButtons.Length == 2 && trayButtons.Length == 2, "Both ANC selectors exist");
        foreach (bool dark in new[] { false, true })
        {
            Console.WriteLine($"ANC UI theme: dark={dark}");
            ThemePalette.Apply(app.Resources, dark);
            device.Emit(new(true, Anc: false)); Drain();
            Check(mainButtons[0].IsChecked == true && trayButtons[0].IsChecked == true, "Initial off state shared by both views");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Click(mainButtons[1]); Drain();
                Check(device.Pending != null && !vm.CanControlAnc, "Click sends command and disables further changes");
                device.Acknowledge(true); Drain();
                Check(vm.CanControlAnc && mainButtons[1].IsChecked == true && mainButtons[0].IsChecked == false && trayButtons[1].IsChecked == true && trayButtons[0].IsChecked == false,
                    "Universal ACK selects on in both windows after a real click");
                Check(vm.AncHint == "ANC je zapnuté.", "Current on status is explicit");
                CheckSelected(mainButtons[1], app); CheckSelected(trayButtons[1], app);
                Click(trayButtons[0]); Drain(); device.Acknowledge(false); Drain();
                Check(mainButtons[0].IsChecked == true && mainButtons[1].IsChecked == false && trayButtons[0].IsChecked == true && trayButtons[1].IsChecked == false,
                    "Universal ACK selects off in both windows after repeated clicks");
                Check(vm.AncHint == "ANC je vypnuté.", "Current off status is explicit");
            }
            device.Emit(device.State with { Anc = true }); Drain();
            Check(mainButtons[1].IsChecked == true && trayButtons[1].IsChecked == true, "External ANC change updates both selectors");
            RenderWindow(main, 424, 611, $"ANC-on-{(dark ? "dark" : "light")}");
            Click(mainButtons[0]); Drain(); device.Fail(); Drain();
            Check(vm.CanControlAnc && mainButtons[1].IsChecked == true && trayButtons[1].IsChecked == true && vm.Error.Length > 0,
                "Command failure preserves confirmed selection");
            device.Emit(new()); Drain();
            Check(!vm.CanControlAnc && mainButtons.All(b => b.IsChecked == false) && trayButtons.All(b => b.IsChecked == false), "Disconnect clears both selections");
        }
        vm.Stop();
    }
    private static void CheckSelected(System.Windows.Controls.RadioButton button, App app)
    {
        button.ApplyTemplate();
        var border = (System.Windows.Controls.Border)button.Template.FindName("B", button);
        Check(((SolidColorBrush)border.BorderBrush).Color == ((SolidColorBrush)app.Resources["Accent"]).Color &&
              ((SolidColorBrush)border.Background).Color == ((SolidColorBrush)app.Resources["Selected"]).Color,
              "Confirmed ANC button has accent border and selected fill");
    }
    // Pump WPF bindings without executing the production startup (mutex, tray, Bluetooth).
    private sealed class TestApp : App
    {
        protected override void OnStartup(StartupEventArgs e) { }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var next in Descendants<T>(child)) yield return next; }
    }
    private static void Click(System.Windows.Controls.RadioButton button) => typeof(System.Windows.Controls.Primitives.ButtonBase)
        .GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(button, null);
    private static void Drain()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background, new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }
    private sealed class FakeBudsService : IBudsService
    {
        public event Action<BudsState>? Changed;
        public BudsState State { get; private set; } = new();
        public TaskCompletionSource? Pending { get; private set; }
        public void Emit(BudsState state) { State = state; Changed?.Invoke(state); }
        public Task SetAncAsync(bool enabled) { Pending = new(); return Pending.Task; }
        public void Acknowledge(bool enabled)
        {
            Emit(BudsProtocol.Apply(State, new(0x42, new byte[] { 0x78, enabled ? (byte)1 : (byte)0 }, true), DateTimeOffset.Now));
            var pending = Pending; Pending = null; pending!.SetResult();
        }
        public void Fail() { var pending = Pending; Pending = null; pending!.SetException(new IOException("Sluchátka nepotvrdila změnu ANC.")); }
        public void Reconnect() { }
    }
    private static double Contrast(Color a, Color b)
    {
        static double Channel(byte value) { double c = value / 255d; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        static double Light(Color c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        double x = Light(a), y = Light(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }
    private static void RenderWindow(Window window, int width, int height, string name)
    {
        var root = (FrameworkElement)window.Content!;
        root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        { dc.DrawRectangle(window.Background, null, new Rect(0, 0, width, height)); }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(drawing); bitmap.Render(root);
        string path = Path.GetFullPath($"TestResults/{name}.png"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); encoder.Save(file);
        Console.WriteLine("UI render: " + path);
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
    private static void DiagnoseAnc()
    {
        System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
        using var service = new BluetoothBudsService();
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        timer.Tick += (_, _) => frame.Continue = false;
        bool started = false;
        bool changedConfirmed = false, restoredConfirmed = false;
        service.Trace += Console.WriteLine;
        service.Changed += async state =>
        {
            Console.WriteLine($"STATE connected={state.Connected} ANC={state.Anc} {state.Status}");
            if (started || !state.Connected || !state.Anc.HasValue) return;
            started = true;
            bool original = state.Anc.Value;
            await Task.Delay(1000);
            try
            {
                Console.WriteLine($"TEST ANC={!original}");
                await service.SetAncAsync(!original);
                changedConfirmed = true;
                Console.WriteLine("CONFIRMED change");
            }
            catch (Exception ex) { Console.WriteLine("CHANGE FAILED: " + ex.Message); }
            await Task.Delay(1500);
            try
            {
                Console.WriteLine($"RESTORE ANC={original}");
                await service.SetAncAsync(original);
                restoredConfirmed = true;
                Console.WriteLine("CONFIRMED restore");
            }
            catch (Exception ex) { Console.WriteLine("RESTORE UNCONFIRMED: " + ex.Message); }
            await Task.Delay(1000);
            frame.Continue = false;
        };
        timer.Start(); service.Start(); System.Windows.Threading.Dispatcher.PushFrame(frame); timer.Stop();
        Check(started && changedConfirmed && restoredConfirmed, "Hardware ANC toggle and restore both confirmed");
    }
}

