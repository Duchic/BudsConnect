using Localization = BudsConnect.Services.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
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
        preferences = Preferences.Load(); Localization.Apply(preferences.Language);
        mutex = new Mutex(true, @"Local\BudsConnect", out bool first);
        if (!first) { MessageBox.Show(Localization.Get("AlreadyRunning"), "BudsConnect"); Shutdown(); return; }
        ApplyTheme();
        bluetooth = new BluetoothBudsService();
        model = new MainViewModel(bluetooth, preferences);
        main = new MainWindow(model); MainWindow = main;
        flyout = new TrayWindow(model);
        tray = new TrayService(() => { if (flyout.IsVisible) flyout.Hide(); else flyout.ShowNearCursor(); }, ShowMain, ExitApplication);
        model.StateChanged += state => tray.Update(state, preferences.LowBatteryAlert, darkTaskbar, model.Name);
        tray.Update(model.State, false, darkTaskbar, model.Name);
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
        ThemePalette.Apply(Resources, dark);
        if (model != null) tray?.Update(model.State, false, darkTaskbar, model.Name);
    }
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Write(e.Exception);
        MessageBox.Show(Localization.Get("AppError"), "BudsConnect");
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

