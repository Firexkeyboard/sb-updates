using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RuriLib;
using IOPath = System.IO;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockAkmCookies : Page
{
    public PageBlockAkmCookies(BlockAkmCookies block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x00, 0xB0, 0xB9)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(AkamaiLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "Akamai Cookie Solver",
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

        AddField(fields, "Website URL", "Url", "https://example.com/page");
        AddField(fields, "Output Variable (cookies)", "OutputCookies", "AKM_COOKIES");
        AddField(fields, "Output Variable (user agent)", "OutputUserAgent", "AKM_UA");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Proxy  —  optional (host:port:user:pass)", "Proxy", "");
        AddField(fields, "Server Port  (default: 8085)", "Port", "8085");

        card.Child = fields;
        outer.Children.Add(card);

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

    private static UIElement AkamaiLogo()
    {
        // Try to load the real Akamai logo from the build directory (WebP, Windows 11 WIC codec)
        var logoPath = IOPath.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "akamai-logo.webp");
        if (IOPath.File.Exists(logoPath))
        {
            try
            {
                var bmp = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
                // Aspect ratio of logo: ~2.45:1; at height 28 → width ~69
                var img = new Image
                {
                    Source  = bmp,
                    Height  = 28,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // White rounded pill so the white-background logo blends cleanly
                return new Border
                {
                    Background      = Brushes.White,
                    CornerRadius    = new CornerRadius(5),
                    Padding         = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child           = img
                };
            }
            catch { /* fall through to drawn paths */ }
        }

        // ── Fallback: drawn wave + text ───────────────────────────────────────
        var blue   = new SolidColorBrush(Color.FromRgb(0x1E, 0x9F, 0xD4));
        var orange = new SolidColorBrush(Color.FromRgb(0xF5, 0xA2, 0x1E));

        var vb     = new Viewbox { Width = 80, Height = 32 };
        var canvas = new Canvas  { Width = 80, Height = 28 };

        canvas.Children.Add(new Path
        {
            Fill = blue,
            Data = Geometry.Parse(
                "M 3 24 C 2 20 1 14 4 9 C 6 5 9 2 13 1" +
                " C 10 3 8 6 7 10 C 5 15 6 21 8 25 Z")
        });
        canvas.Children.Add(new Path
        {
            Fill = blue,
            Data = Geometry.Parse(
                "M 8 25 C 9 22 9 18 10 14 C 11 10 13 7 15 4" +
                " C 13 5 11 8 10 12 C 9 16 10 21 11 25 Z")
        });
        canvas.Children.Add(new Path
        {
            Fill = blue,
            Data = Geometry.Parse(
                "M 11 25 C 12 22 12 18 13 14.5 C 14 11 15.5 8 18 5" +
                " C 16 7 14.5 10 14 13.5 C 13 17.5 13.5 22 14 25 Z")
        });

        var label = new TextBlock
        {
            Text       = "Akamai",
            Foreground = orange,
            FontSize   = 13,
            FontWeight = FontWeights.Bold,
            FontStyle  = FontStyles.Italic,
        };
        Canvas.SetLeft(label, 21);
        Canvas.SetTop(label, 6);
        canvas.Children.Add(label);

        vb.Child = canvas;
        return vb;
    }
}
