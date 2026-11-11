using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BudsConnect.ViewModels;

namespace BudsConnect;

public partial class MainWindow : Window
{
    private readonly MainViewModel model;
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
    public static void OpenSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Nastavení Windows"); }
    }
}
