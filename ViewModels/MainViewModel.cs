using Localization = BudsConnect.Services.Localization;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BudsConnect.Core;
using BudsConnect.Services;

namespace BudsConnect.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IBudsService service;
    private readonly AudioService audio = new();
    private readonly DispatcherTimer refresh;
    private bool busy;
    private string error = "";
    private int? volume;
    public BudsState State { get; private set; } = new();
    public Preferences Preferences { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<BudsState>? StateChanged;
    public string Name => Preferences.DeviceAlias.Length > 0 ? Preferences.DeviceAlias : State.Name;
    public string WindowTitle => Preferences.DeviceAlias.Length > 0 ? $"{Name} · BudsConnect" : "BudsConnect";
    public string TrayTitle => Localization.Get("NamedTrayTitle", Name);
    public void RefreshPreferences() { Notify(nameof(Name)); Notify(nameof(WindowTitle)); Notify(nameof(TrayTitle)); }
    public string Status => Localization.Translate(State.Status);
    public string Left => BudsState.Percent(State.Left);
    public string Right => BudsState.Percent(State.Right);
    public string Case => BudsState.Percent(State.Case);
    public int LeftLevel => State.Left ?? 0;
    public int RightLevel => State.Right ?? 0;
    public int CaseLevel => State.Case ?? 0;
    public bool AncOn => State.Anc == true;
    public bool AncOff => State.Anc == false;
    public bool CanControlAnc => State.Connected && State.Anc.HasValue && !busy;
    public string AncHint => Localization.Translate(busy ? "Čekám na potvrzení sluchátek…" : State.Anc switch
    {
        true => "ANC je zapnuté.",
        false => "ANC je vypnuté.",
        _ => "Ovládání bude dostupné po načtení stavu sluchátek."
    });
    public string Updated => State.Updated is { } at ? Localization.Get("Updated", at.LocalDateTime.ToString("HH:mm:ss")) : Localization.Get("BatteryWaiting");
    public string Error => Localization.Translate(error);
    public Visibility ErrorVisibility => error.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    public bool CanControlVolume => volume.HasValue;
    public string VolumeText => volume.HasValue ? $"{volume} %" : "—";
    public string VolumeHint => volume.HasValue ? "" : Localization.Get("VolumeHint");
    public double Volume
    {
        get => volume ?? 0;
        set
        {
            int next = (int)Math.Round(value);
            if (!volume.HasValue || next == volume) return;
            try { audio.SetVolume(next); volume = next; Notify(nameof(VolumeText)); }
            catch (Exception ex) { ShowError(ex.Message); PollVolume(); }
        }
    }

    public MainViewModel(IBudsService service, Preferences preferences)
    {
        this.service = service;
        Preferences = preferences;
        Localization.Changed += OnLanguageChanged;
        service.Changed += state => Application.Current.Dispatcher.BeginInvoke(() =>
        {
            State = state;
            Notify("");
            StateChanged?.Invoke(state);
        });
        refresh = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => PollVolume(), Application.Current.Dispatcher);
        PollVolume();
    }
    private void PollVolume()
    {
        int? next = audio.ReadVolume();
        if (next == volume) return;
        volume = next;
        Notify(nameof(Volume)); Notify(nameof(VolumeText)); Notify(nameof(CanControlVolume)); Notify(nameof(VolumeHint));
    }
    public async Task SetAncAsync(bool enabled)
    {
        if (!CanControlAnc) return;
        busy = true; error = ""; Notify("");
        try { await service.SetAncAsync(enabled); }
        catch (Exception ex) { AppLog.Write(ex); ShowError(ex.Message); }
        finally { busy = false; Notify(""); }
    }
    public void Reconnect() { error = ""; Notify(""); service.Reconnect(); }
    public void ShowError(string message) { error = message; Notify(nameof(Error)); Notify(nameof(ErrorVisibility)); }
    private void OnLanguageChanged() => Notify("");
    public void Stop() { refresh.Stop(); Localization.Changed -= OnLanguageChanged; }
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

