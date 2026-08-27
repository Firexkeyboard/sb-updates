using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockAltcha : Page
{
    public PageBlockAltcha(BlockAltcha block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0xFF)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(AltchaLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "ALTCHA Proof-of-Work Solver",
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

        AddField(fields, "Challenge URL", "ChallengeUrl", "https://example.com/api/altcha/challenge/");
        AddField(fields, "Output Variable", "OutputVariable", "ALTCHA_TOKEN");

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

    private static UIElement AltchaLogo()
    {
        var vb   = new Viewbox { Width = 40, Height = 40 };
        var g    = new LinearGradientBrush
        {
            StartPoint    = new Point(0.88, 0.40),
            EndPoint      = new Point(0.34, 0.90),
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0x66, 0x66, 0xFF), 0.0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0x24, 0x24, 0xEB), 1.0));

        var path = new Path
        {
            Fill    = g,
            Stretch = Stretch.Uniform,
            Data    = Geometry.Parse(
                "M4.34136 3.80776C9.48297 -0.452317 17.1485 0.209996 21.4627 5.28675" +
                "C22.6234 6.65253 23.4231 8.19876 23.8722 9.81234L25.1549 9.53882" +
                "C25.6909 9.42451 26.1444 9.9389 25.957 10.4487L24.3163 14.9113" +
                "C24.1419 15.3857 23.5282 15.5201 23.1677 15.1627L19.7532 11.7782" +
                "C19.3634 11.3918 19.5603 10.7319 20.0997 10.6169L21.4909 10.3201" +
                "C21.1255 9.0762 20.4981 7.88541 19.6008 6.82945" +
                "C16.1494 2.76808 10.0169 2.23844 5.9036 5.64626" +
                "C1.79033 9.05412 1.25409 15.1093 4.70547 19.1706" +
                "C8.15703 23.2321 14.2894 23.7618 18.4027 20.3539" +
                "C18.4096 20.3482 18.4168 20.3422 18.4242 20.336" +
                "C18.7143 20.092 19.1454 20.098 19.4099 20.3688L20.1279 21.1039" +
                "C20.3986 21.3811 20.3887 21.8259 20.0962 22.0804" +
                "C20.0487 22.1218 20.0039 22.1601 19.9651 22.1923" +
                "C14.8235 26.4523 7.15792 25.79 2.84354 20.7133" +
                "C-1.47068 15.6366 -0.800239 8.0679 4.34136 3.80776Z" +
                "M9.07553 9.97769C9.34779 10.247 9.34074 10.6809 9.12507 10.9964" +
                "C8.16758 12.3972 8.31751 14.3188 9.57341 15.5611" +
                "C10.8293 16.8035 12.7718 16.9518 14.1878 16.0046" +
                "C14.5068 15.7913 14.9454 15.7843 15.2177 16.0536L16.4434 17.2661" +
                "C14.0748 19.6092 10.2309 19.6056 7.85799 17.2583" +
                "C5.48481 14.9107 5.48131 11.1084 7.84995 8.76533L9.07553 9.97769Z")
        };

        vb.Child = path;
        return vb;
    }
}
