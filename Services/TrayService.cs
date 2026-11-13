using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BudsConnect.Core;

namespace BudsConnect.Services;
public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon notify;
    private Icon? icon;
    private bool lowAlerted;
    private string lastIconKey = "";
    private BudsState lastState = new();
    private bool lastDark;
    private readonly ToolStripItem openItem;
    private readonly ToolStripItem exitItem;
    public TrayService(Action showPanel, Action showMain, Action exit)
    {
        var menu = new ContextMenuStrip();
        openItem = menu.Items.Add(Localization.Get("Open"), null, (_, _) => showMain());
        menu.Items.Add(new ToolStripSeparator());
        exitItem = menu.Items.Add(Localization.Get("Exit"), null, (_, _) => exit());
        notify = new NotifyIcon { Text = Localization.Get("TrayDisconnected"), ContextMenuStrip = menu };
        Localization.Changed += OnLanguageChanged;
        notify.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) showPanel(); };
        notify.DoubleClick += (_, _) => showMain();
        Update(new(), false, false);
        notify.Visible = true;
    }
    public void Update(BudsState state, bool alerts, bool darkTaskbar)
    {
        lastState = state; lastDark = darkTaskbar;
        int? level = state.TrayBattery;
        string label = level?.ToString() ?? "–";
        string key = label + darkTaskbar;
        if (lastIconKey != key)
        {
            var next = Draw(label, level < 20, darkTaskbar);
            notify.Icon = next;
            icon?.Dispose(); icon = next; lastIconKey = key;
        }
        string text = state.Connected
            ? Localization.Get("TrayDetails", BudsState.Percent(state.Left), BudsState.Percent(state.Right), BudsState.Percent(state.Case))
            : Localization.Get("TrayDisconnected");
        notify.Text = text.Length > 127 ? text[..127] : text;
        if (alerts && level is < 20 && !lowAlerted)
        {
            notify.ShowBalloonTip(6000, Localization.Get("LowTitle"), Localization.Get("LowBody", level!.Value), ToolTipIcon.Info);
            lowAlerted = true;
        }
        // Keep the latch across disconnects to avoid repeated alerts every reconnect.
        if (level >= 25) lowAlerted = false;
    }
    private static Icon Draw(string label, bool low, bool dark)
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI", label.Length > 2 ? 15 : 20, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(low ? (dark ? Color.FromArgb(255, 173, 110) : Color.FromArgb(165, 58, 10)) : (dark ? Color.White : Color.FromArgb(32, 37, 44)));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, font, brush, new RectangleF(-1, 0, 34, 32), format);
        IntPtr handle = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(handle); return (Icon)borrowed.Clone(); }
        finally { DestroyIcon(handle); }
    }
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
    private void OnLanguageChanged()
    {
        openItem.Text = Localization.Get("Open"); exitItem.Text = Localization.Get("Exit");
        Update(lastState, false, lastDark);
    }
    public void Dispose() { Localization.Changed -= OnLanguageChanged; notify.Visible = false; notify.ContextMenuStrip?.Dispose(); notify.Dispose(); icon?.Dispose(); }
}
