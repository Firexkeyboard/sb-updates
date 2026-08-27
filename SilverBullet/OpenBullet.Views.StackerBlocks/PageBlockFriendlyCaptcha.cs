using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockFriendlyCaptcha : Page
{
    public PageBlockFriendlyCaptcha(BlockFriendlyCaptcha block)
    {
        DataContext = block;
        Background  = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ──────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x25)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0xFA, 0x81, 0x00)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(14, 12, 14, 12),
            Margin          = new Thickness(0, 0, 0, 1)
        };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(FriendlyCaptchaLogo());
        headerRow.Children.Add(new TextBlock
        {
            Text              = "FriendlyCaptcha Solver",
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

        AddField(fields, "Site Key", "SiteKey", "FCMGEMH000000001");

        // ── EU Endpoint checkbox ─────────────────────────────────────────────
        fields.Children.Add(new TextBlock
        {
            Text       = "Use EU Endpoint",
            Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xAA)),
            FontSize   = 11,
            Margin     = new Thickness(0, 12, 0, 4)
        });
        var chk = new CheckBox
        {
            Content = new TextBlock
            {
                Text       = "EU Endpoint (eu-api.friendlycaptcha.eu)",
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xD6)),
                FontSize   = 12
            },
            Foreground = Brushes.White,
            Margin     = new Thickness(0, 0, 0, 4)
        };
        chk.SetBinding(CheckBox.IsCheckedProperty, new Binding("UseEuEndpoint")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        fields.Children.Add(chk);

        fields.Children.Add(new Rectangle
        {
            Height = 1,
            Fill   = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            Margin = new Thickness(0, 14, 0, 4)
        });

        AddField(fields, "Output Variable", "OutputVariable", "FRCAPTCHA_TOKEN");

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

    private static UIElement FriendlyCaptchaLogo()
    {
        var vb = new Viewbox { Width = 40, Height = 40 };
        var g  = new LinearGradientBrush
        {
            StartPoint = new Point(0.226, 0.792),
            EndPoint   = new Point(0.792, 0.171)
        };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFA, 0x81, 0x00), 0.0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xF2, 0xA8, 0x08), 1.0));

        var path = new Path
        {
            Fill    = g,
            Stretch = Stretch.Uniform,
            Data    = Geometry.Parse(
                "M100 5C152.467 5 195 47.533 195 100C195 152.467 152.467 195 100 195" +
                "C47.533 195 5 152.467 5 100C5 87.0516 7.59738 74.6758 12.3125 63.3926" +
                "C15.0891 56.7492 22.7243 53.6175 29.3682 56.3926" +
                "C36.0124 59.1691 39.152 66.8039 36.376 73.4482" +
                "C32.9683 81.6029 31.0781 90.5646 31.0781 100" +
                "C31.0781 138.064 61.9357 168.922 100 168.922" +
                "C138.064 168.922 168.922 138.064 168.922 100" +
                "C168.922 61.9357 138.064 31.0781 100 31.0781" +
                "C92.7986 31.0781 86.9609 25.2404 86.9609 18.0391" +
                "C86.961 10.8378 92.7987 5 100 5Z" +
                "M74.4229 78.0771C84.5125 78.0771 92.6921 86.2561 92.6924 96.3457" +
                "C92.6924 106.436 84.5127 114.615 74.4229 114.615" +
                "C64.3331 114.615 56.1543 106.435 56.1543 96.3457" +
                "C56.1545 86.2562 64.3333 78.0773 74.4229 78.0771Z" +
                "M125.577 78.0771C135.667 78.0773 143.845 86.2562 143.846 96.3457" +
                "C143.846 106.435 135.667 114.615 125.577 114.615" +
                "C115.487 114.615 107.308 106.436 107.308 96.3457" +
                "C107.308 86.2561 115.487 78.0771 125.577 78.0771Z")
        };

        vb.Child = path;
        return vb;
    }
}
