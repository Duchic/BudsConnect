using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BudsConnect.ViewModels;

namespace BudsConnect;

public partial class MainWindow : Window
{
    private readonly MainViewModel model;
    private Size classicSize = new(440, 650);
    public bool IsMiniMode { get; private set; }
    public MainWindow(MainViewModel model)
    {
        this.model = model; InitializeComponent(); DataContext = model;
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/BudsConnect;component/Assets/BudsConnect.ico"));
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.Exiting && model.Preferences.CloseToTray) { e.Cancel = true; Hide(); }
        else if (!App.Exiting) { e.Cancel = true; ((App)Application.Current).ExitApplication(); }
        base.OnClosing(e);
    }
    private async void AncOff_Click(object sender, RoutedEventArgs e) => await model.SetAncAsync(false);
    private async void AncOn_Click(object sender, RoutedEventArgs e) => await model.SetAncAsync(true);
    private void Reconnect_Click(object sender, RoutedEventArgs e) => model.Reconnect();
    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow(model) { Owner = this }.ShowDialog();
    private void Bluetooth_Click(object sender, RoutedEventArgs e) => OpenSettings("ms-settings:bluetooth");
    private void Mini_Click(object sender, RoutedEventArgs e) => SetMiniMode(true);
    private void Expand_Click(object sender, RoutedEventArgs e) => SetMiniMode(false);
    private async void MiniAnc_Click(object sender, RoutedEventArgs e) => await model.SetAncAsync(!model.AncOn);
    public void SetMiniMode(bool mini)
    {
        if (IsMiniMode == mini) return;
        if (mini) classicSize = new Size(Width, Height);
        IsMiniMode = mini;
        ClassicView.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
        MiniView.Visibility = mini ? Visibility.Visible : Visibility.Collapsed;
        MinWidth = mini ? 270 : 400;
        MinHeight = mini ? 250 : 540;
        Width = mini ? 286 : classicSize.Width;
        Height = mini ? 290 : classicSize.Height;
        // Keep the expanded window reachable if the mini window was moved near an edge.
        if (IsLoaded)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var area = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
            var matrix = System.Windows.PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice
                ?? System.Windows.Media.Matrix.Identity;
            var start = matrix.Transform(new Point(area.Left, area.Top));
            var end = matrix.Transform(new Point(area.Right, area.Bottom));
            Left = Math.Clamp(Left, start.X, Math.Max(start.X, end.X - Width));
            Top = Math.Clamp(Top, start.Y, Math.Max(start.Y, end.Y - Height));
        }
        if (mini) ExpandButton.Focus(); else MiniButton.Focus();
    }
    public static void OpenSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Nastavení Windows"); }
    }
}
