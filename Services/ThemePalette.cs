using System.Windows;
using System.Windows.Media;

namespace BudsConnect.Services;

public static class ThemePalette
{
    public static void Apply(ResourceDictionary resources, bool dark)
    {
        string[] names = { "Surface", "Text", "Muted", "Soft", "Line", "Accent", "Selected" };
        string[] light = { "#FCFCFD", "#20252C", "#646B76", "#F0F2F6", "#E4E7ED", "#315EE5", "#E9EFFF" };
        string[] night = { "#24272D", "#F1F3F6", "#ABB2BF", "#30353E", "#383D46", "#A5BAFF", "#354364" };
        for (int i = 0; i < names.Length; i++)
            resources[names[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString((dark ? night : light)[i]));
    }
}
