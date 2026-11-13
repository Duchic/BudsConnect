using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace BudsConnect.Services;

public sealed class Preferences
{
    public bool CloseToTray { get; set; } = true;
    public bool LowBatteryAlert { get; set; } = true;
    public string Theme { get; set; } = "System";
    private string language = "en";
    public string Language { get => language; set => language = Localization.Normalize(value); }
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudsConnect");
    private static string FilePath => Path.Combine(DataDirectory, "settings.json");
    public static Preferences Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? new() : new(); }
        catch (Exception ex) { AppLog.Write(ex); return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
    }
    public static bool StartsWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue("BudsConnect") is string;
    }
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("BudsConnect", $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue("BudsConnect", false);
    }
}

public static class AppLog
{
    private static readonly object Gate = new();
    public static void Write(Exception ex)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Preferences.DataDirectory);
                string path = Path.Combine(Preferences.DataDirectory, "app.log");
                if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) File.Move(path, path + ".old", true);
                File.AppendAllText(path, $"{DateTimeOffset.Now:O} {ex}\n");
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
