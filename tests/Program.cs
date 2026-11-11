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
}

