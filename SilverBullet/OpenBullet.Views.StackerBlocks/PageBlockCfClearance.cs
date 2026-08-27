using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockCfClearance : Page
{
    public PageBlockCfClearance(BlockCfClearance block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0xF3, 0x80, 0x20)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(CfLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "Cloudflare CF Clearance Solver",
            FontSize          = 18,
            FontWeight        = FontWeights.Bold,
            Foreground        = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(12, 0, 0, 0)
        });
        header.Child = headerRow;
        outer.Children.Add(header);

        // ── Fields card ─────────────────────────────────────────────────────
        var card = new Border
        {
            Background   = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            CornerRadius = new CornerRadius(0, 0, 8, 8),
            Padding      = new Thickness(14, 8, 14, 16)
        };
        var fields = new StackPanel();

        AddField(fields, "Website URL", "Url", "https://example.com/login");
        AddField(fields, "Output Variable (cf_clearance)", "OutputVariable", "CF_CLEARANCE");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Timeout (seconds, default: 30)", "Timeout", "30");
        AddField(fields, "Server Port  (default: 9516)", "Port", "9516");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 8)
        });

        var cbRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var cb = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground        = Brushes.White,
        };
        cb.SetBinding(CheckBox.IsCheckedProperty, new Binding("StoreCookies")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        cbRow.Children.Add(cb);
        cbRow.Children.Add(new TextBlock
        {
            Text              = "Store all cookies in bot cookie jar",
            Foreground        = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xAA)),
            FontSize          = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(8, 0, 0, 0)
        });
        fields.Children.Add(cbRow);

        card.Child = fields;
        outer.Children.Add(card);

        outer.Children.Add(new TextBlock
        {
            Text         = "Bypasses Cloudflare's bot protection and returns the cf_clearance cookie. Uses Chrome (nodriver) with a pre-warmed browser pool.",
            Foreground   = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x65)),
            FontSize     = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin       = new Thickness(2, 8, 2, 0)
        });

        scroll.Content = outer;
        Content        = scroll;
    }

    private static void AddField(StackPanel parent, string labelText, string bindingPath, string placeholder)
    {
        parent.Children.Add(new TextBlock
        {
            Text       = labelText,
            Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xAA)),
            FontSize   = 11,
            Margin     = new Thickness(0, 12, 0, 4)
        });
        var tb = new TextBox
        {
            Padding         = new Thickness(8, 6, 8, 6),
            Background      = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x16)),
            Foreground      = Brushes.White,
            CaretBrush      = Brushes.White,
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x42)),
            BorderThickness = new Thickness(1),
            FontSize        = 12,
            ToolTip         = placeholder
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        parent.Children.Add(tb);
    }

    private static UIElement CfLogo()
    {
        var vb     = new Viewbox { Width = 40, Height = 40 };
        var canvas = new Canvas  { Width = 40, Height = 40 };

        var grad = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint   = new System.Windows.Point(1, 1)
        };
        grad.GradientStops.Add(new GradientStop(Color.FromRgb(0xF3, 0x80, 0x20), 0.0));
        grad.GradientStops.Add(new GradientStop(Color.FromRgb(0xFB, 0xAD, 0x41), 1.0));

        canvas.Children.Add(new Rectangle
        {
            Width   = 40,
            Height  = 40,
            RadiusX = 9,
            RadiusY = 9,
            Fill    = grad
        });

        canvas.Children.Add(new TextBlock
        {
            Text       = "CF",
            Foreground = Brushes.White,
            FontSize   = 17,
            FontWeight = FontWeights.Bold,
            Margin     = new Thickness(7, 9, 0, 0)
        });

        vb.Child = canvas;
        return vb;
    }

}
