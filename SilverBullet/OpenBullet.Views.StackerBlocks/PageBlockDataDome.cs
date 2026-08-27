using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;
using WinPoint = System.Windows.Point;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockDataDome : Page
{
    public PageBlockDataDome(BlockDataDome block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x08, 0xD8, 0xCA)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(DataDomeLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "DataDome Cookie Solver",
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
        AddField(fields, "Output Variable (cookie string)", "OutputCookie", "DD_COOKIE");
        AddField(fields, "Output Variable (user agent)",    "OutputUserAgent", "DD_UA");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Proxy  —  optional (host:port:user:pass)", "Proxy", "");
        AddField(fields, "Server Port  (default: 9505)", "Port", "9505");

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

    private static UIElement DataDomeLogo()
    {
        // Exact transcription of data-dome.svg (30×30 viewBox, two evenodd paths,
        // each with its own absolute linearGradient #46C1F7 → #08D8CA)
        var vb     = new Viewbox { Width = 36, Height = 36 };
        var canvas = new Canvas  { Width = 30, Height = 30 };

        // Gradient for path 0: diagonal top-right → bottom-left
        var grad0 = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(0x46, 0xC1, 0xF7), 0.0),
                new GradientStop(Color.FromRgb(0x08, 0xD8, 0xCA), 1.0)
            })
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint  = new WinPoint(23.6, 2.98),
            EndPoint    = new WinPoint(6.4,  22.76)
        };

        // Gradient for path 1: slightly shifted
        var grad1 = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(0x46, 0xC1, 0xF7), 0.0),
                new GradientStop(Color.FromRgb(0x08, 0xD8, 0xCA), 1.0)
            })
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint  = new WinPoint(17.34, 8.98),
            EndPoint    = new WinPoint(0.27,  29.5)
        };

        // Path 0 — main "D" body with hollow centre (evenodd = F1)
        canvas.Children.Add(new Path
        {
            Fill = grad0,
            Data = Geometry.Parse(
                "F1 M7 1H6V24H7H9H17C23.3513 24 28.5 18.8513 28.5 12.5C28.5 6.14873 " +
                "23.3513 1 17 1H9H7Z" +
                "M9 4V21H17C21.6944 21 25.5 17.1944 25.5 12.5C25.5 7.80558 21.6944 4 17 4H9Z")
        });

        // Path 1 — second overlapping "D" shape
        canvas.Children.Add(new Path
        {
            Fill = grad1,
            Data = Geometry.Parse(
                "F1 M16.2424 26H17C18.0578 26 19.0823 25.8572 20.0552 25.5897C17.95 28.2748 " +
                "14.6765 30 11 30H3H1H0V7H1H3V10V11V27H11C12.4476 27 13.8106 26.6382 15.0036 26" +
                "H16.1939C16.21 26 16.2262 26 16.2424 26Z" +
                "M19.4945 18.1913C19.332 13.6399 15.5911 10 11 10V7C16.5933 7 21.2189 10.7387 " +
                "22.2517 16.0298C21.7117 16.782 20.3666 17.7469 19.4945 18.1913Z")
        });

        vb.Child = canvas;
        return vb;
    }
}
