using Localization = BudsConnect.Services.Localization;
using System;
using System.Windows;
using System.Windows.Controls;
using BudsConnect.Services;
using BudsConnect.ViewModels;

namespace BudsConnect;
public partial class SettingsWindow : Window
{
    private readonly MainViewModel model;
    public SettingsWindow(MainViewModel model)
    {
        this.model = model; InitializeComponent();
        StartupBox.IsChecked = Preferences.StartsWithWindows();
        TrayBox.IsChecked = model.Preferences.CloseToTray;
        AlertBox.IsChecked = model.Preferences.LowBatteryAlert;
        ThemeBox.SelectedIndex = model.Preferences.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        LanguageBox.SelectedIndex = model.Preferences.Language == "cs" ? 1 : 0;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Preferences.StartsWithWindows() != (StartupBox.IsChecked == true)) Preferences.SetStartup(StartupBox.IsChecked == true);
            model.Preferences.CloseToTray = TrayBox.IsChecked == true;
            model.Preferences.LowBatteryAlert = AlertBox.IsChecked == true;
            model.Preferences.Theme = (string)((ComboBoxItem)ThemeBox.SelectedItem).Tag;
            model.Preferences.Language = (string)((ComboBoxItem)LanguageBox.SelectedItem).Tag;
            model.Preferences.Save(); Localization.Apply(model.Preferences.Language);
            ((App)Application.Current).ApplyTheme(); Close();
        }
        catch (Exception ex) { MessageBox.Show(this, Localization.Get("SaveError", ex.Message), "BudsConnect"); }
    }
}

