using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockTurnstile : Page
{
    public PageBlockTurnstile(BlockTurnstile block)
    {
        DataContext = block;
        Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new StackPanel { Margin = new Thickness(14) };

        // ── Header card ─────────────────────────────────────────────────────
        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0x82, 0x12)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius = new CornerRadius(8, 8, 0, 0),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(CloudLogo());

        headerRow.Children.Add(new TextBlock
        {
            Text = "Cloudflare Turnstile Solver",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        });
        header.Child = headerRow;
        outer.Children.Add(header);

        // ── Fields card ─────────────────────────────────────────────────────
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            CornerRadius = new CornerRadius(0, 0, 8, 8),
            Padding = new Thickness(14, 8, 14, 16)
        };
        var fields = new StackPanel();

        AddField(fields, "Domain", "Domain", "https://example.com/login");
        AddField(fields, "Site Key", "SiteKey", "0x4AAAAAAA...");
        AddField(fields, "Output Variable (token)", "OutputVariable", "TURNSTILE_TOKEN");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Action  —  optional, only if the site requires it", "Action", "login");
        AddDisabledField(fields, "Browser Proxy  —  coming in a future update", "Proxy");
        AddField(fields, "Server Port  (default: 8742)", "Port", "8742");

        card.Child = fields;
        outer.Children.Add(card);

        scroll.Content = outer;
        Content = scroll;
    }

    private static void AddDisabledField(StackPanel parent, string labelText, string bindingPath)
    {
        parent.Children.Add(new TextBlock
        {
            Text = labelText,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x65)),
            FontSize = 11,
            Margin = new Thickness(0, 12, 0, 4)
        });

        var tb = new TextBox
        {
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x16)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x55)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x30)),
            BorderThickness = new Thickness(1),
            FontSize = 11,
            IsEnabled = false,
            ToolTip = "Proxy support is coming in a future update"
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        parent.Children.Add(tb);
    }

    private static void AddField(StackPanel parent, string labelText, string bindingPath, string placeholder)
    {
        parent.Children.Add(new TextBlock
        {
            Text = labelText,
            Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xAA)),
            FontSize = 11,
            Margin = new Thickness(0, 12, 0, 4)
        });

        var tb = new TextBox
        {
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x16)),
            Foreground = Brushes.White,
            CaretBrush = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x42)),
            BorderThickness = new Thickness(1),
            FontSize = 12,
            ToolTip = placeholder
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        parent.Children.Add(tb);
    }

    private static UIElement CloudLogo()
    {
        var vb = new Viewbox { Width = 40, Height = 40 };
        var canvas = new Canvas { Width = 26, Height = 26 };

        // Gradient matching SVG: #FFB756 → #E17216
        var gradient = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(3.46667, 9.53346),
            EndPoint = new Point(24.7, 19.9335)
        };
        gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xB7, 0x56), 0.0));
        gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xE1, 0x72, 0x16), 1.0));

        // Cloud body — exact path from SVG (fill-rule: evenodd → F1 prefix)
        canvas.Children.Add(new Path
        {
            Fill = gradient,
            Data = Geometry.Parse(
                "F1 M21.5949 11.4011C21.6422 11.0743 21.6667 10.7401 21.6667 10.4001" +
                "C21.6667 6.57096 18.5625 3.4668 14.7333 3.4668" +
                "C11.4451 3.4668 8.69156 5.75583 7.97916 8.82734" +
                "C7.64916 8.72305 7.29782 8.6668 6.93333 8.6668" +
                "C5.01875 8.6668 3.46667 10.2189 3.46667 12.1335" +
                "C3.46667 12.4836 3.51857 12.8216 3.6151 13.1402" +
                "C1.53889 13.6552 0 15.5313 0 17.7668" +
                "C0 20.2533 1.90385 22.2951 4.33333 22.514V22.5335H4.76667H20.3667H20.8" +
                "V22.517C23.7087 22.2958 26 19.8655 26 16.9001" +
                "C26 14.2108 24.1155 11.9616 21.5949 11.4011Z")
        });

        // Diagonal stroke — white
        canvas.Children.Add(new Path
        {
            Fill = Brushes.White,
            Data = Geometry.Parse(
                "F1 M20.2708 13.3617C20.5495 13.5476 20.6249 13.9242 20.439 14.203" +
                "L16.9723 19.403C16.7865 19.6818 16.4098 19.7571 16.1311 19.5713" +
                "C15.8523 19.3854 15.7769 19.0088 15.9628 18.73" +
                "L19.4295 13.53C19.6153 13.2512 19.992 13.1759 20.2708 13.3617Z")
        });

        // Horizontal bar — white
        canvas.Children.Add(new Path
        {
            Fill = Brushes.White,
            Data = Geometry.Parse(
                "F1 M8.92822 16.466C8.92822 16.131 9.19984 15.8594 9.53489 15.8594" +
                "H22.5349C22.8699 15.8594 23.1416 16.131 23.1416 16.466" +
                "C23.1416 16.8011 22.8699 17.0727 22.5349 17.0727" +
                "H9.53489C9.19984 17.0727 8.92822 16.8011 8.92822 16.466Z")
        });

        vb.Child = canvas;
        return vb;
    }
}
