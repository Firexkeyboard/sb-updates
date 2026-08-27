using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace OpenBullet.Views.Main;

public class HomePage : Page
{
    private Button    _updateBtn;
    private TextBlock _bodyText;

    public HomePage()
    {
        Background = Brushes.Transparent;
        BuildContent();
        Loaded += (_, _) => Task.Run(CheckForUpdatesAsync);
    }

    // ── COLOR SHORTCUTS ───────────────────────────────────────────────────
    private static SolidColorBrush C(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));
    private static readonly SolidColorBrush Orange   = new(Color.FromRgb(230, 100,  0));
    private static readonly SolidColorBrush CardBg   = new(Color.FromRgb( 24,  24, 24));
    private static readonly SolidColorBrush CardBrd  = new(Color.FromRgb( 38,  38, 38));
    private static readonly SolidColorBrush PageBg   = new(Color.FromRgb( 15,  15, 15));
    private static readonly SolidColorBrush TextMute = new(Color.FromRgb(140, 140, 140));

    private void BuildContent()
    {
        // Full-page grid — card is vertically centered
        var outer = new Grid { Background = Brushes.Transparent };
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ── Central content ───────────────────────────────────────────────
        var card = new StackPanel
        {
            Orientation         = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            MaxWidth            = 860,
            Margin              = new Thickness(60, 36, 60, 36),
        };
        Grid.SetRow(card, 0);
        outer.Children.Add(card);

        // ── Title ─────────────────────────────────────────────────────────
        var title = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            FontSize      = 28,
            FontFamily    = new FontFamily("Segoe UI"),
            Margin        = new Thickness(0, 0, 0, 24),
            TextWrapping  = TextWrapping.Wrap,
        };
        title.Inlines.Add(new System.Windows.Documents.Run("Welcome to ")
        {
            Foreground = C(220, 220, 220),
            FontWeight = FontWeights.Normal,
        });
        title.Inlines.Add(new System.Windows.Documents.Run("SilverBullet X")
        {
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
        });
        card.Children.Add(title);

        // ── Buttons ───────────────────────────────────────────────────────
        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 0, 0, 20),
        };

        // Changelog — dark bg, blue text
        var btnChangelog = MakeColoredButton(
            "☰  Show Changelog",
            C(20, 20, 23), C(32, 32, 38), C(100, 160, 255));
        btnChangelog.Margin = new Thickness(0, 0, 10, 0);
        btnChangelog.Click += (_, _) => ScrollBody();
        btnRow.Children.Add(btnChangelog);

        // Check for Updates — dark bg, orange text
        var btnCheck = MakeColoredButton(
            "↻  Check for Updates",
            C(20, 20, 23), C(32, 32, 38), Orange);
        btnCheck.Click += (_, _) =>
        {
            _bodyText.Text = "Checking for updates…";
            Task.Run(CheckForUpdatesAsync);
        };
        btnRow.Children.Add(btnCheck);
        card.Children.Add(btnRow);

        // ── Update button (hidden until needed) ───────────────────────────
        _updateBtn = new Button
        {
            Content         = "↻  Update Available! Click here to update",
            FontSize        = 11.5,
            FontFamily      = new FontFamily("Segoe UI"),
            FontWeight      = FontWeights.SemiBold,
            Foreground      = C(255, 200, 60),
            Background      = new SolidColorBrush(Color.FromArgb(30, 255, 180, 0)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(120, 255, 180, 0)),
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(20, 8, 20, 8),
            Margin          = new Thickness(0, 0, 0, 16),
            Visibility      = Visibility.Collapsed,
            Cursor          = System.Windows.Input.Cursors.Hand,
        };
        _updateBtn.Click += UpdateBtn_Click;
        card.Children.Add(_updateBtn);

        // ── Body text ─────────────────────────────────────────────────────
        _bodyText = new TextBlock
        {
            Text          = "Loading release notes…",
            FontSize      = 11.5,
            FontFamily    = new FontFamily("Segoe UI"),
            Foreground    = TextMute,
            TextWrapping  = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            LineHeight    = 20,
        };
        card.Children.Add(_bodyText);

        Content = outer;
    }

    private static Button MakeButton(string text, Color bg, Color border, Color fore) =>
        new Button
        {
            Content = text, FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(fore),
            Background = new SolidColorBrush(bg), BorderBrush = new SolidColorBrush(border),
            BorderThickness = new Thickness(1.5), Padding = new Thickness(18, 7, 18, 7),
            Margin = new Thickness(6, 0, 6, 0), Cursor = System.Windows.Input.Cursors.Hand,
        };

    // Bypasses MahApps template so the actual Background color is rendered
    private static Button MakeColoredButton(string content, SolidColorBrush bg, SolidColorBrush bgHover, SolidColorBrush fg)
    {
        var bdFac = new System.Windows.FrameworkElementFactory(typeof(Border));
        bdFac.Name = "Bd";
        bdFac.SetValue(Border.BackgroundProperty, bg);
        bdFac.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)));
        bdFac.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        bdFac.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        var cp = new System.Windows.FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(ContentPresenter.MarginProperty, new Thickness(22, 9, 22, 9));
        cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        bdFac.AppendChild(cp);
        var tpl = new ControlTemplate(typeof(Button)) { VisualTree = bdFac };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, bgHover, "Bd"));
        tpl.Triggers.Add(hover);
        return new Button
        {
            Content     = content,
            Template    = tpl,
            Foreground  = fg,
            FontSize    = 12,
            FontFamily  = new FontFamily("Segoe UI"),
            FontWeight  = FontWeights.SemiBold,
            Cursor      = System.Windows.Input.Cursors.Hand,
        };
    }

    // ── Update check ───────────────────────────────────────────────────────
    private void CheckForUpdatesAsync()
    {
        try
        {
            var rel      = CheckUpdate.Run<LatestRelease>("https://api.github.com/repos/mohamm4dx/SilverBullet/releases/latest");
            bool hasUpd  = rel?.Available == true;
            string body  = rel?.Body ?? "";
            string rName = rel?.Name ?? "";

            Dispatcher.Invoke(() =>
            {
                if (hasUpd)
                {
                    _updateBtn.Content    = $"↻  {rName} is available! Click here to update";
                    _updateBtn.Visibility = Visibility.Visible;
                    if (SB.MainWindow?.updateButton != null)
                        SB.MainWindow.updateButton.Visibility = Visibility.Visible;
                }

                SetBodyText(body, rName);
            });
        }
        catch
        {
            Dispatcher.Invoke(() => SetBodyText("", ""));
        }
    }

    private void SetBodyText(string body, string relName)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            body = "SilverBullet X is the new version of OPB, but with a complete engine for LOLICODE, " +
                   "allowing bidirectional operation, serialization, and much more. " +
                   "It also supports IronPython, JavaScript, and C++ (being compatible with their syntax). " +
                   "It includes its own local solvers and other tools.";
        }
        else
        {
            // Strip markdown link syntax and bullet chars for clean display
            body = System.Text.RegularExpressions.Regex.Replace(body, @"\[([^\]]+)\]\([^)]+\)", "$1");
            body = body.Replace("**", "").Replace("•", "·").Trim();
        }

        if (!string.IsNullOrWhiteSpace(relName))
            body = relName + "\n\n" + body;

        _bodyText.Text = body;
    }

    private void UpdateBtn_Click(object sender, RoutedEventArgs e)
    {
        try { SB.MainWindow?.UpdateButton_Click(sender, e); } catch { }
    }

    private void ScrollBody() { /* changelog is the body text itself */ }

    private static void TryOpen(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}
