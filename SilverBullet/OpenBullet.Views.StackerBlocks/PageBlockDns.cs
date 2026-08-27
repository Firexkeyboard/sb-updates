using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockDns : Page
{
    private readonly BlockDns vm;

    public PageBlockDns(BlockDns block)
    {
        vm    = block;
        Title = "PageBlockDns";
        BuildUI();
    }

    private void BuildUI()
    {
        var fg      = Utils.GetBrush("ForegroundMain");
        var accent  = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));   // gris medio
        var dimText = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));   // gris claro

        var root = new StackPanel();

        // ── Header bar ───────────────────────────────────────────────────────
        var headerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            Padding    = new Thickness(14, 10, 14, 10),
        };
        var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
        headerStack.Children.Add(new TextBlock
        {
            Text              = "DNS LOOKUP",
            FontSize          = 15,
            FontWeight        = FontWeights.Bold,
            Foreground        = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
        });
        headerStack.Children.Add(new TextBlock
        {
            Text              = "  ·  DNS Resolution Block",
            FontSize          = 11,
            Foreground        = dimText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 2, 0, 0),
        });
        headerBorder.Child = headerStack;
        root.Children.Add(headerBorder);

        // ── Body ─────────────────────────────────────────────────────────────
        var body = new StackPanel { Margin = new Thickness(14, 8, 14, 14) };

        // ── BLOCK section ────────────────────────────────────────────────────
        body.Children.Add(MakeSectionHeader("BLOCK", accent));
        body.Children.Add(MakeFormRow("Label",
            MakeStretchTextBox(() => vm.Label, v => vm.Label = v), fg));

        // ── OUTPUT section ───────────────────────────────────────────────────
        body.Children.Add(MakeSectionHeader("OUTPUT", accent));

        var isCaptureChk = new CheckBox
        {
            Content           = "Is Capture",
            IsChecked         = vm.IsCapture,
            Foreground        = fg,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 2, 0, 6),
        };
        isCaptureChk.Checked   += (s, e) => vm.IsCapture = true;
        isCaptureChk.Unchecked += (s, e) => vm.IsCapture = false;
        body.Children.Add(isCaptureChk);

        body.Children.Add(MakeFormRow("Variable Name",
            MakeStretchTextBox(() => vm.VariableName, v => vm.VariableName = v), fg));

        // ── QUERY SETTINGS section ───────────────────────────────────────────
        body.Children.Add(MakeSectionHeader("QUERY SETTINGS", accent));

        body.Children.Add(MakeFormRow("Query",
            MakeStretchTextBox(() => vm.Query, v => vm.Query = v), fg));

        // Record Type
        var recCombo = new ComboBox { Width = 90, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (string n in Enum.GetNames(typeof(DnsRecordType))) recCombo.Items.Add(n);
        recCombo.SelectedIndex     = (int)vm.RecordType;
        recCombo.SelectionChanged += (s, e) =>
        {
            if (recCombo.SelectedIndex >= 0)
                vm.RecordType = (DnsRecordType)recCombo.SelectedIndex;
        };
        body.Children.Add(MakeFormRow("Record Type", recCombo, fg));

        // Transport
        var transCombo = new ComboBox { Width = 175, HorizontalAlignment = HorizontalAlignment.Left };
        transCombo.Items.Add("UDP");
        transCombo.Items.Add("TCP");
        transCombo.Items.Add("DNS over HTTPS");
        transCombo.SelectedIndex     = (int)vm.Transport;
        transCombo.SelectionChanged += (s, e) =>
        {
            if (transCombo.SelectedIndex >= 0)
                vm.Transport = (DnsTransport)transCombo.SelectedIndex;
        };
        body.Children.Add(MakeFormRow("Transport", transCombo, fg));

        body.Children.Add(MakeFormRow("Server (optional)",
            MakeStretchTextBox(() => vm.Server, v => vm.Server = v), fg));

        var timeoutBox = new TextBox
        {
            Text                = vm.TimeoutMs.ToString(),
            Width               = 90,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
            Padding             = new Thickness(4, 3, 4, 3),
        };
        timeoutBox.TextChanged += (s, e) =>
        {
            if (int.TryParse(timeoutBox.Text, out int t)) vm.TimeoutMs = t;
        };
        body.Children.Add(MakeFormRow("Timeout (ms)", timeoutBox, fg));

        // ── Info box ─────────────────────────────────────────────────────────
        body.Children.Add(new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(4),
            Padding         = new Thickness(10, 8, 10, 8),
            Margin          = new Thickness(0, 14, 0, 0),
            Child           = new TextBlock
            {
                Text = "Queries DNS records for a given name.\n" +
                       "Results are stored as a ListOfStrings variable.\n" +
                       "Transports: UDP, TCP (default server 8.8.8.8) or DNS over HTTPS.\n" +
                       "Types: A, AAAA, CAA, CNAME, MX, NAPTR, NS, PTR, SOA, SRV, TLSA, TXT, URI.",
                Foreground   = dimText,
                TextWrapping = TextWrapping.Wrap,
                FontSize     = 11,
            }
        });

        root.Children.Add(body);

        // ── ScrollViewer con soporte rueda de ratón ───────────────────────────
        var scroll = new ScrollViewer
        {
            Content                       = root,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        scroll.PreviewMouseWheel += (s, e) =>
        {
            if (!e.Handled)
            {
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        };

        Content = scroll;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Border MakeSectionHeader(string title, Brush lineColor)
    {
        var b = new Border
        {
            BorderBrush     = lineColor,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin          = new Thickness(0, 12, 0, 6),
            Padding         = new Thickness(0, 0, 0, 3),
        };
        b.Child = new TextBlock
        {
            Text       = title,
            FontSize   = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
        };
        return b;
    }

    private static Grid MakeFormRow(string label, UIElement control, Brush fg)
    {
        var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock
        {
            Text              = label,
            Foreground        = fg,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize          = 12,
        };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(control, 1);
        g.Children.Add(lbl);
        g.Children.Add(control);
        return g;
    }

    private static TextBox MakeStretchTextBox(Func<string> getter, Action<string> setter)
    {
        var tb = new TextBox
        {
            Text                = getter(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Center,
            Padding             = new Thickness(4, 3, 4, 3),
        };
        tb.TextChanged += (s, e) => setter(tb.Text);
        return tb;
    }
}
