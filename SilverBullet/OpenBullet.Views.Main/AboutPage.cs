using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace OpenBullet.Views.Main;

public class AboutPage : Page, IComponentConnector
{
    internal Button docuButton;
    private bool _contentLoaded;

    public AboutPage()
    {
        InitializeComponent();
        BuildContent();
    }

    private void BuildContent()
    {
        // ── SilverBullet palette ──────────────────────────────────────────────
        var cBg      = Color.FromRgb(30, 30, 30);
        var cCard    = Color.FromRgb(38, 38, 38);
        var cBorder  = Color.FromRgb(58, 58, 58);
        var cText    = new SolidColorBrush(Color.FromRgb(215, 213, 225));
        var cMuted   = new SolidColorBrush(Color.FromRgb(145, 143, 158));
        var cWhite   = new SolidColorBrush(Colors.White);
        var cOrange  = new SolidColorBrush(Color.FromRgb(230, 100, 0));
        var cGold    = new SolidColorBrush(Color.FromRgb(255, 195, 40));
        var cRed     = new SolidColorBrush(Color.FromRgb(218, 50, 50));
        var cGreen   = new SolidColorBrush(Color.FromRgb(55, 190, 90));

        this.Background = new SolidColorBrush(cBg);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = new SolidColorBrush(cBg),
        };

        // Outer grid to centre content horizontally
        var outer = new Grid { Background = new SolidColorBrush(cBg) };
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(780, GridUnitType.Pixel) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var page = new StackPanel
        {
            Margin = new Thickness(0, 40, 0, 40),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        Grid.SetColumn(page, 1);
        outer.Children.Add(page);

        // ── Logo ──────────────────────────────────────────────────────────────
        try
        {
            var bmp = new BitmapImage(
                new Uri("pack://application:,,,/SilverBullet;component/images/icons/sbicon.png"));
            page.Children.Add(new System.Windows.Controls.Image
            {
                Source              = bmp,
                Width               = 100,
                Height              = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 20),
                Effect              = new DropShadowEffect
                {
                    Color       = Color.FromRgb(230, 100, 0),
                    BlurRadius  = 36,
                    ShadowDepth = 0,
                    Opacity     = 0.6,
                },
            });
        }
        catch { }

        // ── Title ─────────────────────────────────────────────────────────────
        page.Children.Add(new TextBlock
        {
            Text              = "About SilverBullet X",
            FontSize          = 26,
            FontWeight        = FontWeights.Bold,
            Foreground        = cWhite,
            TextAlignment     = TextAlignment.Center,
            Margin            = new Thickness(0, 0, 0, 6),
        });
        page.Children.Add(new TextBlock
        {
            Text          = "Next-Generation Credential Checking Suite",
            FontSize      = 13,
            Foreground    = cMuted,
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 28),
        });

        page.Children.Add(Divider(cBorder));

        // ── Description ───────────────────────────────────────────────────────
        var desc = new TextBlock
        {
            TextWrapping  = TextWrapping.Wrap,
            FontSize      = 13.5,
            Foreground    = cText,
            TextAlignment = TextAlignment.Center,
            LineHeight    = 24,
            Margin        = new Thickness(0, 22, 0, 24),
        };
        desc.Inlines.Add(new Run("SilverBullet X") { FontWeight = FontWeights.Bold, Foreground = cWhite });
        desc.Inlines.Add(new Run(" is an automation suite powered by .NET. It allows performing requests towards a target webapp and offers a lot of tools to work with the results. This software can be used for "));
        desc.Inlines.Add(new Run("scraping") { Foreground = cOrange, FontWeight = FontWeights.Bold });
        desc.Inlines.Add(new Run(", "));
        desc.Inlines.Add(new Run("parsing") { Foreground = cOrange, FontWeight = FontWeights.Bold });
        desc.Inlines.Add(new Run(" data, automated "));
        desc.Inlines.Add(new Run("pentesting") { Foreground = cGold, FontWeight = FontWeights.Bold });
        desc.Inlines.Add(new Run(" and much more. It is a mod version with more features and customization."));
        page.Children.Add(desc);

        // ── Legal Notice ──────────────────────────────────────────────────────
        var legalStack = new StackPanel();
        var legalHdr = new TextBlock { FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) };
        legalHdr.Inlines.Add(new Run("⚠  ") { Foreground = cRed });
        legalHdr.Inlines.Add(new Run("IMPORTANT LEGAL NOTICE") { Foreground = cRed });
        legalStack.Children.Add(legalHdr);
        legalStack.Children.Add(new TextBlock
        {
            Text         = "Performing (D)DoS attacks or credential stuffing on sites you do not own (or do not have permission to test) is illegal! The developer will not be held responsible for improper use of this software.",
            TextWrapping = TextWrapping.Wrap,
            FontSize     = 13,
            Foreground   = cText,
            LineHeight   = 22,
        });
        page.Children.Add(new Border
        {
            Background      = new SolidColorBrush(Color.FromArgb(22, 218, 50, 50)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(115, 218, 50, 50)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(7),
            Padding         = new Thickness(20, 14, 20, 14),
            Margin          = new Thickness(0, 0, 0, 28),
            Child           = legalStack,
        });

        page.Children.Add(Divider(cBorder));

        // ── Developer Credits ─────────────────────────────────────────────────
        var devRow = new WrapPanel { Margin = new Thickness(0, 22, 0, 8) };
        devRow.Children.Add(Tb("Developer:   ", cMuted, 13.5));
        devRow.Children.Add(Tb("Firexkeyboard / Venom", cOrange, 13.5, bold: true));
        page.Children.Add(devRow);

        var thanksRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 28) };
        thanksRow.Children.Add(Tb("Special Thanks to:   ", cMuted, 13));
        thanksRow.Children.Add(Tb("• ", cMuted, 13));
        thanksRow.Children.Add(Tb("Ruri (Made OpenBullet)", cOrange, 13, bold: true));
        thanksRow.Children.Add(Tb("   •   ", cMuted, 13));
        thanksRow.Children.Add(Tb("@Mohamm4dx (Made SilverBullet)", cOrange, 13, bold: true));
        page.Children.Add(thanksRow);

        // ── License box ───────────────────────────────────────────────────────
        var licRow = new StackPanel { Orientation = Orientation.Horizontal };
        licRow.Children.Add(new TextBlock
        {
            Text              = "🔑  ",
            FontSize          = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground        = cGreen,
        });
        var licTb = new TextBlock
        {
            TextWrapping      = TextWrapping.Wrap,
            FontSize          = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground        = cText,
        };
        licTb.Inlines.Add(new Run("This software is licensed under the "));
        licTb.Inlines.Add(new Run("MIT license") { FontWeight = FontWeights.Bold, Foreground = cWhite });
        licTb.Inlines.Add(new Run(". Please visit the Telegram using the button below."));
        licRow.Children.Add(licTb);
        page.Children.Add(new Border
        {
            Background      = new SolidColorBrush(Color.FromArgb(20, 55, 190, 90)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(95, 55, 190, 90)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(7),
            Padding         = new Thickness(20, 14, 20, 14),
            Margin          = new Thickness(0, 0, 0, 32),
            Child           = licRow,
        });

        // ── Action buttons (Border-based so colors always render) ─────────────
        var btnRow = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        btnRow.Children.Add(ActionBtn("$   Donate",
            Color.FromRgb(185, 68, 0), Color.FromRgb(230, 100, 0),
            "https://github.com/mohamm4dx/SilverBullet"));
        btnRow.Children.Add(ActionBtn("⊙   Repository",
            Color.FromRgb(22, 100, 42), Color.FromRgb(40, 155, 65),
            "https://github.com/mohamm4dx/SilverBullet"));
        btnRow.Children.Add(ActionBtn("✦   Report An Issue",
            Color.FromRgb(58, 35, 115), Color.FromRgb(100, 62, 178),
            "https://github.com/mohamm4dx/SilverBullet/issues"));
        btnRow.Children.Add(ActionBtn("➤   Telegram",
            Color.FromRgb(8, 90, 165), Color.FromRgb(18, 132, 215),
            "https://t.me/venomsiteoficial"));
        page.Children.Add(btnRow);

        scroll.Content  = outer;
        this.Content    = scroll;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Border Divider(Color color) => new Border
    {
        Height     = 1,
        Background = new SolidColorBrush(color),
    };

    private static TextBlock Tb(string text, SolidColorBrush fg,
        double size = 13, bool bold = false) => new TextBlock
    {
        Text              = text,
        FontSize          = size,
        FontWeight        = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground        = fg,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static Border ActionBtn(string text, Color bg, Color hover, string url)
    {
        var normalBg = new SolidColorBrush(Color.FromArgb(30, bg.R, bg.G, bg.B));
        var hoverBg  = new SolidColorBrush(Color.FromArgb(200, bg.R, bg.G, bg.B));

        var tb = new TextBlock
        {
            Text                = text,
            FontSize            = 13,
            FontWeight          = FontWeights.SemiBold,
            Foreground          = new SolidColorBrush(Colors.White),
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var btn = new Border
        {
            Background      = normalBg,
            BorderBrush     = new SolidColorBrush(Color.FromArgb(220, bg.R, bg.G, bg.B)),
            BorderThickness = new Thickness(1.5),
            CornerRadius    = new CornerRadius(22),
            Padding         = new Thickness(28, 12, 28, 12),
            Margin          = new Thickness(6, 0, 6, 0),
            Cursor          = Cursors.Hand,
            Child           = tb,
            Effect          = new DropShadowEffect
            {
                Color       = bg,
                BlurRadius  = 0,
                ShadowDepth = 0,
                Opacity     = 0,
            },
        };

        var normalEffect = new DropShadowEffect { Color = bg, BlurRadius = 0,  ShadowDepth = 0, Opacity = 0   };
        var hoverEffect  = new DropShadowEffect { Color = bg, BlurRadius = 18, ShadowDepth = 0, Opacity = 0.7 };

        btn.MouseLeftButtonUp += (s, e) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        };
        btn.MouseEnter += (s, e) => { btn.Background = hoverBg;  btn.Effect = hoverEffect;  };
        btn.MouseLeave += (s, e) => { btn.Background = normalBg; btn.Effect = normalEffect; };
        return btn;
    }

    // ── BAML wiring ───────────────────────────────────────────────────────────
    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
        if (!_contentLoaded)
        {
            _contentLoaded = true;
            Uri resourceLocator = new Uri("/SilverBullet;component/views/main/aboutpage.xaml", UriKind.Relative);
            Application.LoadComponent(this, resourceLocator);
        }
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target)
    {
        if (connectionId == 1)
            docuButton = (Button)target;
        else
            _contentLoaded = true;
    }
}
