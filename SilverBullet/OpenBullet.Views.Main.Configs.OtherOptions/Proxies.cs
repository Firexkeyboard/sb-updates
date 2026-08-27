using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace OpenBullet.Views.Main.Configs.OtherOptions;

public class Proxies : Page
{
    public Proxies()
    {
        base.DataContext = SB.ConfigManager.CurrentConfig.Config.Settings;
        BuildUI();
    }

    private void BuildUI()
    {
        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { Margin = new Thickness(10) };
        scrollViewer.Content = panel;
        base.Content = scrollViewer;

        // Needs Proxies
        panel.Children.Add(MakeBoolRow("Needs Proxies", "NeedsProxies"));

        // Only SOCKS
        panel.Children.Add(MakeBoolRow("Only SOCKS Proxies", "OnlySocks"));

        // Only SSL
        panel.Children.Add(MakeBoolRow("Only SSL Proxies", "OnlySsl"));

        // Max Proxy Uses
        panel.Children.Add(MakeIntRow("Max Proxy Uses", "MaxProxyUses", 0, 99999));

        // Ban Proxy After Good Status
        panel.Children.Add(MakeBoolRow("Ban Proxy After Good Status", "BanProxyAfterGoodStatus"));

        // Ban Loop Evasion Override
        panel.Children.Add(MakeIntRow("Ban Loop Evasion Override (-1 = off)", "BanLoopEvasionOverride", -1, 99999));

        // ── Sticky Proxy section ──────────────────────────────────────────
        panel.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 4) });

        panel.Children.Add(new Label
        {
            Content = "STICKY PROXY SESSION",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(0, 2, 0, 4)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "When enabled, all requests from the same bot use the same outgoing IP. " +
                   "The suffix is appended to the proxy username (e.g. user → user-sessid-a1b2c3d4). " +
                   "Use {0} as a placeholder for the random session ID.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(2, 0, 2, 8)
        });

        // Enable Sticky Proxy
        panel.Children.Add(MakeBoolRow("Enable Sticky Proxy (same IP per config run)", "StickyProxy"));

        // Session Suffix
        panel.Children.Add(MakeTextRow("Session suffix (e.g. -sessid-{0})", "StickyProxySuffix", 280));
    }

    private static UIElement MakeBoolRow(string label, string propertyName)
    {
        var cb = new CheckBox
        {
            Content = label,
            Margin = new Thickness(2, 5, 2, 5),
            VerticalAlignment = VerticalAlignment.Center
        };
        cb.SetBinding(CheckBox.IsCheckedProperty,
            new Binding(propertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        return cb;
    }

    private static UIElement MakeIntRow(string label, string propertyName, int min, int max)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 5, 2, 5) };
        row.Children.Add(new Label
        {
            Content = label + ":",
            Width = 240,
            VerticalAlignment = VerticalAlignment.Center
        });
        var updown = new IntegerUpDown
        {
            Width = 90,
            Minimum = min,
            Maximum = max,
            VerticalAlignment = VerticalAlignment.Center
        };
        updown.SetBinding(IntegerUpDown.ValueProperty,
            new Binding(propertyName)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new IntToNullableIntConverter()
            });
        row.Children.Add(updown);
        return row;
    }

    private static UIElement MakeTextRow(string label, string propertyName, double textBoxWidth)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 5, 2, 5) };
        row.Children.Add(new Label
        {
            Content = label + ":",
            Width = 240,
            VerticalAlignment = VerticalAlignment.Center
        });
        var tb = new TextBox
        {
            Width = textBoxWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        tb.SetBinding(TextBox.TextProperty,
            new Binding(propertyName) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        row.Children.Add(tb);
        return row;
    }
}

// Xceed IntegerUpDown uses int? (nullable) — ConfigSettings uses int
internal class IntToNullableIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int i) return (int?)i;
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null) return 0;
        try { return System.Convert.ToInt32(value); }
        catch { return 0; }
    }
}
