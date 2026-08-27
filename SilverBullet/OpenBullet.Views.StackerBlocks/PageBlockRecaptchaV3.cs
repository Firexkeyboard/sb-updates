using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockRecaptchaV3 : Page
{
    public PageBlockRecaptchaV3(BlockRecaptchaV3 block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x42, 0x85, 0xF4)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(RecaptchaLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "Google reCAPTCHA v3 Solver",
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
        AddField(fields, "Site Key", "SiteKey", "6LcXXXXXXXXXXXXXXXXXXXXXXX");
        AddField(fields, "Output Variable (token)", "OutputVariable", "RC3_TOKEN");

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Action  —  default: submit", "Action", "submit");
        AddField(fields, "Server Port  (default: 9512)", "Port", "9512");

        card.Child = fields;
        outer.Children.Add(card);

        outer.Children.Add(new TextBlock
        {
            Text         = "Tip: visit http://localhost:9512/setup once to log into Google for better scores.",
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

    private static UIElement RecaptchaLogo()
    {
        var vb     = new Viewbox { Width = 40, Height = 40 };
        var canvas = new Canvas  { Width = 26, Height = 26 };

        // ── Path 1: top-right arrow  (#D3DDFF → #B4C4FF) ───────────────────
        var g1 = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint  = new System.Windows.Point(25.9985, 12.5357),
            EndPoint    = new System.Windows.Point(12.5342, 1.85714)
        };
        g1.GradientStops.Add(new GradientStop(Color.FromRgb(0xD3, 0xDD, 0xFF), 0.0));
        g1.GradientStops.Add(new GradientStop(Color.FromRgb(0xB4, 0xC4, 0xFF), 1.0));

        canvas.Children.Add(new Path
        {
            Fill = g1,
            Data = Geometry.Parse(
                "F1 M25.2899 12.9737C25.6808 12.9769 25.999 12.6613 25.9985 12.2704" +
                "L25.9875 3.07938C25.987 2.63387 25.448 2.41146 25.1335 2.72694" +
                "L23.0775 4.78892C20.697 1.86655 17.0756 0 13.0193 0" +
                "C8.79803 0 5.04787 2.02077 2.67725 5.15025L7.44481 9.98201" +
                "C7.91203 9.1154 8.57577 8.37107 9.37607 7.80889" +
                "C10.2084 7.15746 11.3877 6.62483 13.0191 6.62483" +
                "C13.2162 6.62483 13.3683 6.64793 13.4801 6.69144" +
                "C15.5014 6.85145 17.2535 7.97021 18.2851 9.59401" +
                "L15.7657 12.1208C15.4508 12.4366 15.6757 12.9756 16.1217 12.9741" +
                "C19.6103 12.9619 23.2514 12.9566 25.2899 12.9737Z")
        });

        // ── Path 2: left arrow  (#98AFFF → #7C99FF) ────────────────────────
        var g2 = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint  = new System.Windows.Point(12.5357, 0),
            EndPoint    = new System.Windows.Point(1.39286, 13.464)
        };
        g2.GradientStops.Add(new GradientStop(Color.FromRgb(0x98, 0xAF, 0xFF), 0.0));
        g2.GradientStops.Add(new GradientStop(Color.FromRgb(0x7C, 0x99, 0xFF), 1.0));

        canvas.Children.Add(new Path
        {
            Fill = g2,
            Data = Geometry.Parse(
                "F1 M12.9359 0.709416C12.9392 0.318539 12.6236 0.000384907 12.2327 0.000854838" +
                "L3.06896 0.0118715C2.62415 0.0124062 2.40143 0.549922 2.7155 0.864909" +
                "L4.77499 2.93041C1.86112 5.31788 0 8.94985 0 13.018" +
                "C0 17.2515 2.01492 21.0126 5.13527 23.3902L9.95297 18.6087" +
                "C9.08888 18.1401 8.34671 17.4744 7.78617 16.6718" +
                "C7.13667 15.837 6.60556 14.6543 6.60556 13.0181" +
                "C6.60556 12.8205 6.62859 12.6679 6.67198 12.5558" +
                "C6.83151 10.5286 7.94702 8.77142 9.5661 7.73677" +
                "L12.082 10.26C12.3974 10.5764 12.9379 10.3518 12.9363 9.90501" +
                "C12.9242 6.40547 12.9189 2.75326 12.9359 0.709416Z")
        });

        // ── Path 3: bottom arrow  (#708FFA → #355CE7) ──────────────────────
        var g3 = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint  = new System.Windows.Point(0, 12.9997),
            EndPoint    = new System.Windows.Point(21.8214, 19.9639)
        };
        g3.GradientStops.Add(new GradientStop(Color.FromRgb(0x70, 0x8F, 0xFA), 0.0));
        g3.GradientStops.Add(new GradientStop(Color.FromRgb(0x35, 0x5C, 0xE7), 1.0));

        canvas.Children.Add(new Path
        {
            Fill = g3,
            Data = Geometry.Parse(
                "F1 M0.709442 13.0254C0.318547 13.0221 0.000382703 13.3378 0.000849925 13.7287" +
                "L0.0118352 22.9197C0.0123677 23.3652 0.551335 23.5876 0.865903 23.2721" +
                "L2.92188 21.2101C5.30241 24.1325 8.92382 25.9991 12.9801 25.9991" +
                "C16.953 25.9991 20.5086 24.2091 22.8895 21.3895" +
                "C23.146 21.0858 23.115 20.6389 22.8357 20.3559" +
                "L19.1246 16.5948C18.8413 16.3076 18.36 16.3771 18.1245 16.7047" +
                "C17.711 17.2803 17.203 17.783 16.6233 18.1902" +
                "C15.791 18.8416 14.6116 19.3742 12.9803 19.3742" +
                "C12.7832 19.3742 12.631 19.3511 12.5193 19.3076" +
                "C10.498 19.1476 8.7459 18.0289 7.71427 16.4051" +
                "L10.2337 13.8783C10.5486 13.5624 10.3237 13.0234 9.87764 13.025" +
                "C6.38904 13.0371 2.74802 13.0425 0.709442 13.0254Z")
        });

        vb.Child = canvas;
        return vb;
    }
}
