using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using BudsConnect.ViewModels;

namespace BudsConnect;
public partial class TrayWindow : Window
{
    private readonly MainViewModel model;
    public TrayWindow(MainViewModel model)
    {
        this.model = model; InitializeComponent(); DataContext = model;
        Deactivated += (_, _) => Hide();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Hide(); };
    }
    public void ShowNearCursor()
    {
        Show(); UpdateLayout();
        var cursor = System.Windows.Forms.Cursor.Position;
        var area = System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        Point min = transform.Transform(new Point(area.Left, area.Top));
        Point max = transform.Transform(new Point(area.Right, area.Bottom));
        Point at = transform.Transform(new Point(cursor.X, cursor.Y));
        Left = Math.Clamp(at.X - ActualWidth / 2, min.X + 8, Math.Max(min.X + 8, max.X - ActualWidth - 8));
        Top = Math.Clamp(at.Y - ActualHeight - 10, min.Y + 8, Math.Max(min.Y + 8, max.Y - ActualHeight - 8));
        Activate();
    }
    private async void AncOff_Click(object sender, RoutedEventArgs e) => await model.SetAncAsync(false);
    private async void AncOn_Click(object sender, RoutedEventArgs e) => await model.SetAncAsync(true);
    private void Open_Click(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).ShowMain(); }
}
