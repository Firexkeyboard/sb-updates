using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockRecaptchaV3Bypass : Page
{
    public PageBlockRecaptchaV3Bypass(BlockRecaptchaV3Bypass block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────────
        var headerGrad = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromRgb(0xFF, 0x66, 0x00), 0.0),
            new GradientStop(Color.FromRgb(0x00, 0xCC, 0x44), 1.0)
        }, new Point(0, 0.5), new Point(1, 0.5));

        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = headerGrad,
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(RecaptchaIcon());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "reCAPTCHA v3 Bypass",
            FontSize          = 18,
            FontWeight        = FontWeights.Bold,
            Foreground        = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(12, 0, 0, 0)
        });
        header.Child = headerRow;
        outer.Children.Add(header);

        // ── Fields card ─────────────────────────────────────────────────────────
        var card = new Border
        {
            Background   = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            CornerRadius = new CornerRadius(0, 0, 8, 8),
            Padding      = new Thickness(14, 8, 14, 16)
        };
        var fields = new StackPanel();

        AddField(fields, "Variable Name",       "VariableName", "RECAPTCHA_TOKEN");
        AddField(fields, "Recaptcha Url (GET)",  "GetUrl",      "https://www.google.com/recaptcha/api2/anchor?ar=1&k=SITEKEY&...");
        AddLargeField(fields, "BG",             "Bg",           "!q62grYx... (optional background token)");
        AddField(fields, "Recaptcha Url (POST)", "PostUrl",     "https://www.google.com/recaptcha/api2/reload?k=SITEKEY");
        AddField(fields, "Referer",             "Referer",      "https://example.com/");
        AddField(fields, "User-Agent",          "UserAgent",    "Mozilla/5.0 ...");

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

    private static void AddLargeField(StackPanel parent, string labelText, string bindingPath, string placeholder)
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
            Padding                      = new Thickness(8, 6, 8, 6),
            Background                   = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x16)),
            Foreground                   = Brushes.White,
            CaretBrush                   = Brushes.White,
            BorderBrush                  = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x42)),
            BorderThickness              = new Thickness(1),
            FontSize                     = 12,
            AcceptsReturn                = false,
            TextWrapping                 = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            ToolTip                      = placeholder
        };
        tb.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        parent.Children.Add(tb);
    }

    private static UIElement RecaptchaIcon()
    {
        var vb = new Viewbox { Width = 36, Height = 36 };
        var grad = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromRgb(0xFF, 0x66, 0x00), 0.0),
            new GradientStop(Color.FromRgb(0x00, 0xCC, 0x44), 1.0)
        }, new Point(0, 0), new Point(1, 1));

        // Shield-like path for reCAPTCHA icon
        var path = new Path
        {
            Fill    = grad,
            Stretch = Stretch.Uniform,
            Data    = Geometry.Parse(
                "M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5z " +
                "M10 17l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9z")
        };
        vb.Child = path;
        return vb;
    }
}
