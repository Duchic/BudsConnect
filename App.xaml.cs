using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using BudsConnect.Services;
using BudsConnect.ViewModels;

namespace BudsConnect;
public partial class App : Application
{
    private Mutex? mutex;
    private BluetoothBudsService? bluetooth;
    private TrayService? tray;
    private TrayWindow? flyout;
    private MainWindow? main;
    private MainViewModel? model;
    private Preferences preferences = new();
    private bool darkTaskbar;
    public static bool Exiting { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        mutex = new Mutex(true, @"Local\BudsConnect", out bool first);
        if (!first) { MessageBox.Show("BudsConnect už běží. Otevři jej kliknutím na ikonu u hodin.", "BudsConnect"); Shutdown(); return; }
        preferences = Preferences.Load(); ApplyTheme();
        bluetooth = new BluetoothBudsService();
        model = new MainViewModel(bluetooth, preferences);
        main = new MainWindow(model); MainWindow = main;
        flyout = new TrayWindow(model);
        tray = new TrayService(() => { if (flyout.IsVisible) flyout.Hide(); else flyout.ShowNearCursor(); }, ShowMain, ExitApplication);
        model.StateChanged += state => tray.Update(state, preferences.LowBatteryAlert, darkTaskbar);
        tray.Update(model.State, false, darkTaskbar);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        DispatcherUnhandledException += OnUnhandledException;
        if (!e.Args.Contains("--background")) main.Show();
        bluetooth.Start();
    }
    public void ShowMain() { flyout?.Hide(); main?.Show(); if (main != null) { main.WindowState = WindowState.Normal; main.Activate(); } }
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(ApplyTheme);
    public void ApplyTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        bool dark = preferences.Theme == "Dark" || (preferences.Theme == "System" && key?.GetValue("AppsUseLightTheme") is int apps && apps == 0);
        darkTaskbar = key?.GetValue("SystemUsesLightTheme") is int system && system == 0;
        string[] names = { "Surface", "Text", "Muted", "Soft", "Line", "Accent", "Selected" };
        string[] light = { "#FCFCFD", "#20252C", "#646B76", "#F0F2F6", "#E4E7ED", "#315EE5", "#E9EFFF" };
        string[] night = { "#24272D", "#F1F3F6", "#ABB2BF", "#30353E", "#383D46", "#A5BAFF", "#354364" };
        for (int i = 0; i < names.Length; i++) Resources[names[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString((dark ? night : light)[i]));
        if (model != null) tray?.Update(model.State, false, darkTaskbar);
    }
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Write(e.Exception);
        MessageBox.Show("V aplikaci nastala chyba. Podrobnosti jsou v %LOCALAPPDATA%\\BudsConnect\\app.log.", "BudsConnect");
        e.Handled = true; ExitApplication();
    }
    public void ExitApplication() { Exiting = true; Shutdown(); }
    protected override void OnExit(ExitEventArgs e)
    {
        Exiting = true;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        model?.Stop(); bluetooth?.Dispose(); tray?.Dispose(); mutex?.Dispose();
        base.OnExit(e);
    }
}
