using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RuriLib;

namespace OpenBullet.Views.StackerBlocks;

public class PageBlockCapMonster : Page
{
    private static readonly (string Display, string Value)[] TaskOptions =
    {
        ("reCAPTCHA v2",            "RecaptchaV2"),
        ("reCAPTCHA v2 Enterprise", "RecaptchaV2Enterprise"),
        ("reCAPTCHA v3",            "RecaptchaV3"),
        ("Cloudflare Turnstile",    "Turnstile"),
        ("GeeTest",                 "GeeTest"),
        ("Image to Text",           "ImageToText"),
        ("Friendly Captcha",        "FriendlyCaptcha"),
        ("Amazon WAF",              "Amazon"),
        ("DataDome Slider",         "DataDome"),
        ("Basilisk / FaucetPay",    "Basilisk"),
        ("FunCaptcha / Arkose",     "FunCaptcha"),
        ("Imperva / Incapsula",     "Imperva"),
    };

    private StackPanel targetSection;
    private StackPanel geeSection;
    private StackPanel imageSection;
    private StackPanel dataDomeSection;
    private StackPanel funCaptchaSection;
    private StackPanel impervaSection;
    private StackPanel v2Section;
    private StackPanel v3Section;
    private StackPanel actionSection;
    private StackPanel dataSection;
    private StackPanel proxySection;
    private CheckBox   cbUseProxy;

    public PageBlockCapMonster(BlockCapMonster block)
    {
        DataContext = block;
        Background  = Bg(0x16, 0x16, 0x1A);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer  = new StackPanel { Margin = new Thickness(14) };

        // ── Header ────────────────────────────────────────────────────────────
        var header = new Border
        {
            Background      = Bg(0x12, 0x0E, 0x24),
            BorderBrush     = Br(0x7D, 0x6E, 0xFF),
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius    = new CornerRadius(8, 8, 0, 0),
            Padding         = new Thickness(16, 14, 16, 14),
            Margin          = new Thickness(0, 0, 0, 1),
        };
        header.Child = Logo();
        outer.Children.Add(header);

        // ── Card ──────────────────────────────────────────────────────────────
        var card = new Border
        {
            Background   = Bg(0x1F, 0x1F, 0x25),
            CornerRadius = new CornerRadius(0, 0, 8, 8),
            Padding      = new Thickness(16, 10, 16, 18),
        };
        var f = new StackPanel();

        // ── API Key ───────────────────────────────────────────────────────────
        Lbl(f, "CapMonster API Key");
        Box(f, "ApiKey", "YOUR_API_KEY_HERE");

        // ── Task Type ─────────────────────────────────────────────────────────
        Lbl(f, "Task Type");
        var combo = new ComboBox
        {
            Background = Bg(0x12, 0x12, 0x16), Foreground = Brushes.White,
            BorderBrush = Br(0x35, 0x35, 0x42), FontSize = 12,
            Padding = new Thickness(6, 5, 6, 5),
        };
        foreach (var (disp, _) in TaskOptions)
            combo.Items.Add(disp);

        int initIdx = Array.FindIndex(TaskOptions, t => t.Value == block.TaskType);
        combo.SelectedIndex = Math.Max(0, initIdx);
        combo.SelectionChanged += (_, _) =>
        {
            int i = combo.SelectedIndex;
            if (i >= 0 && i < TaskOptions.Length)
            {
                block.TaskType = TaskOptions[i].Value;
                RefreshVisibility(block.TaskType);
            }
        };
        f.Children.Add(combo);

        Sep(f);

        // ── Target: Website URL + Site Key ───────────────────────────────────
        targetSection = new StackPanel();
        Lbl(targetSection, "Website URL");
        Box(targetSection, "WebsiteURL", "https://example.com/page-with-captcha");
        Lbl(targetSection, "Site Key");
        Box(targetSection, "WebsiteKey", "6Lc...");
        f.Children.Add(targetSection);

        // ── GeeTest fields ────────────────────────────────────────────────────
        geeSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(geeSection, "Website URL");
        Box(geeSection, "WebsiteURL", "https://example.com");
        Lbl(geeSection, "GT (GeeTest hash)");
        Box(geeSection, "Gt", "022397c99c9f646f6477822485f30404");
        Lbl(geeSection, "Challenge");
        Box(geeSection, "Challenge", "12345678abcdef...");
        f.Children.Add(geeSection);

        // ── DataDome fields ───────────────────────────────────────────────────
        dataDomeSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(dataDomeSection, "Website URL (main page)");
        Box(dataDomeSection, "WebsiteURL", "https://yourwebsite.com/page-with-datadome");
        Lbl(dataDomeSection, "Captcha URL (geo.captcha-delivery.com URL)");
        Box(dataDomeSection, "CaptchaUrl", "https://geo.captcha-delivery.com/captcha/?...");
        Lbl(dataDomeSection, "DataDome Cookie (datadome=...)");
        Box(dataDomeSection, "DatadomeCookie", "datadome=AHrlqA...");
        Lbl(dataDomeSection, "Version (optional — leave empty or set \"new\")");
        Box(dataDomeSection, "DatadomeVersion", "new");
        f.Children.Add(dataDomeSection);

        // ── Imperva / Incapsula fields ────────────────────────────────────────
        impervaSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(impervaSection, "Website URL (main page)");
        Box(impervaSection, "WebsiteURL", "https://yourwebsite.com/page-with-imperva");
        Lbl(impervaSection, "Incapsula Script URL (e.g. _Incapsula_Resource?SWJIYLWA=...)");
        Box(impervaSection, "IncapsulaScriptUrl", "_Incapsula_Resource?SWJIYLWA=...");
        Lbl(impervaSection, "Incapsula Cookies (incap_sess_=...; visid_incap_=...)");
        Box(impervaSection, "IncapsulaCookies", "incap_sess_=...; visid_incap_=...;");
        Lbl(impervaSection, "Reese84 Endpoint (optional)");
        Box(impervaSection, "Reese84UrlEndpoint", "");
        f.Children.Add(impervaSection);

        // ── FunCaptcha extra fields ───────────────────────────────────────────
        funCaptchaSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(funCaptchaSection, "API JS Subdomain (optional — only if different from client-api.arkoselabs.com)");
        Box(funCaptchaSection, "FuncaptchaApiJSSubdomain", "example-api.arkoselabs.com");
        Lbl(funCaptchaSection, "Blob Data (optional — data[blob] value)");
        Box(funCaptchaSection, "CaptchaData", "{\"blob\":\"...\"}");
        f.Children.Add(funCaptchaSection);

        // ── Image to Text field ───────────────────────────────────────────────
        imageSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(imageSection, "Image Body (base64 encoded)");
        var imgBox = new TextBox
        {
            Padding = new Thickness(8, 6, 8, 6), Height = 64,
            Background = Bg(0x12, 0x12, 0x16), Foreground = Brushes.White,
            CaretBrush = Brushes.White, BorderBrush = Br(0x35, 0x35, 0x42),
            BorderThickness = new Thickness(1), FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            ToolTip = "Base64-encoded image (without data:image/... prefix)",
        };
        imgBox.SetBinding(TextBox.TextProperty, Bind("ImageBody"));
        imageSection.Children.Add(imgBox);
        f.Children.Add(imageSection);

        // ── reCAPTCHA v2 specific ─────────────────────────────────────────────
        v2Section = new StackPanel { Visibility = Visibility.Collapsed };
        var cbInvis = new CheckBox
        {
            Content = "Invisible mode (no checkbox, hidden confirmation)",
            Foreground = Br(0xCC, 0xCC, 0xCC), FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0),
        };
        cbInvis.SetBinding(CheckBox.IsCheckedProperty, Bind("IsInvisible"));
        v2Section.Children.Add(cbInvis);
        f.Children.Add(v2Section);

        // ── reCAPTCHA v3 specific ─────────────────────────────────────────────
        v3Section = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(v3Section, "Min Score (0.1 – 0.9)");
        var scoreGrid = new Grid();
        scoreGrid.ColumnDefinitions.Add(new ColumnDefinition());
        scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        var scoreBox = MkBox("MinScore", "0.7");
        Grid.SetColumn(scoreBox, 1); scoreGrid.Children.Add(scoreBox);
        v3Section.Children.Add(scoreGrid);
        var cbEnt = new CheckBox
        {
            Content = "Enterprise mode",
            Foreground = Br(0xCC, 0xCC, 0xCC), FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        };
        cbEnt.SetBinding(CheckBox.IsCheckedProperty, Bind("IsEnterprise"));
        v3Section.Children.Add(cbEnt);
        f.Children.Add(v3Section);

        // ── Page Action (v3 + Turnstile) ──────────────────────────────────────
        actionSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(actionSection, "Page Action (e.g. verify, login, submit)");
        Box(actionSection, "PageAction", "verify");
        f.Children.Add(actionSection);

        // ── Turnstile extra data ───────────────────────────────────────────────
        dataSection = new StackPanel { Visibility = Visibility.Collapsed };
        Lbl(dataSection, "Turnstile Data (pageload token, optional)");
        Box(dataSection, "CaptchaData", "extra data string");
        f.Children.Add(dataSection);

        Sep(f);

        // ── Output variable ───────────────────────────────────────────────────
        Lbl(f, "Output Variable");
        Box(f, "OutputVariable", "CAPMONSTER_TOKEN");

        // ── Poll Delay ────────────────────────────────────────────────────────
        var delayRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        delayRow.ColumnDefinitions.Add(new ColumnDefinition());
        delayRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        var delayLbl = new TextBlock
        {
            Text = "Poll Delay (ms between status checks)",
            Foreground = Br(0x9A, 0x9A, 0xAA), FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(delayLbl, 0); delayRow.Children.Add(delayLbl);
        var delayBox = MkBox("PollDelayMs", "3000");
        Grid.SetColumn(delayBox, 1); delayRow.Children.Add(delayBox);
        f.Children.Add(delayRow);

        Sep(f);

        // ── Use Proxy ─────────────────────────────────────────────────────────
        cbUseProxy = new CheckBox
        {
            Content = "Use Proxy",
            Foreground = Brushes.White, FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
            IsChecked = block.UseProxy,
        };
        cbUseProxy.Checked   += (_, _) => { block.UseProxy = true;  proxySection.Visibility = Visibility.Visible;   };
        cbUseProxy.Unchecked += (_, _) => { block.UseProxy = false; proxySection.Visibility = Visibility.Collapsed; };
        f.Children.Add(cbUseProxy);

        proxySection = new StackPanel { Visibility = block.UseProxy ? Visibility.Visible : Visibility.Collapsed };
        Lbl(proxySection, "Proxy Type");
        var ptCombo = new ComboBox
        {
            Background = Bg(0x12, 0x12, 0x16), Foreground = Brushes.White,
            BorderBrush = Br(0x35, 0x35, 0x42), FontSize = 12,
            Padding = new Thickness(6, 5, 6, 5),
        };
        foreach (var pt in new[] { "http", "https", "socks4", "socks5" })
            ptCombo.Items.Add(pt);
        ptCombo.SelectedItem = block.ProxyType ?? "http";
        if (ptCombo.SelectedIndex < 0) ptCombo.SelectedIndex = 0;
        ptCombo.SelectionChanged += (_, _) => { if (ptCombo.SelectedItem is string v) block.ProxyType = v; };
        proxySection.Children.Add(ptCombo);

        var addrLblGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        addrLblGrid.ColumnDefinitions.Add(new ColumnDefinition());
        addrLblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        addrLblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        var addrLbl = new TextBlock { Text = "Address", Foreground = Br(0x9A, 0x9A, 0xAA), FontSize = 11 };
        Grid.SetColumn(addrLbl, 0); addrLblGrid.Children.Add(addrLbl);
        var portLbl = new TextBlock { Text = "Port", Foreground = Br(0x9A, 0x9A, 0xAA), FontSize = 11 };
        Grid.SetColumn(portLbl, 2); addrLblGrid.Children.Add(portLbl);
        proxySection.Children.Add(addrLblGrid);

        var addrGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        addrGrid.ColumnDefinitions.Add(new ColumnDefinition());
        addrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        addrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        var addrBox = MkBox("ProxyAddress", "1.2.3.4");
        Grid.SetColumn(addrBox, 0); addrGrid.Children.Add(addrBox);
        var portBox = MkBox("ProxyPort", "8080");
        Grid.SetColumn(portBox, 2); addrGrid.Children.Add(portBox);
        proxySection.Children.Add(addrGrid);

        Lbl(proxySection, "Login (optional)");
        Box(proxySection, "ProxyLogin", "username");
        Lbl(proxySection, "Password (optional)");
        Box(proxySection, "ProxyPassword", "password");
        f.Children.Add(proxySection);

        Sep(f);

        // ── Optional Settings ─────────────────────────────────────────────────
        bool hasOptional = !string.IsNullOrEmpty(block.UserAgent)
                        || !string.IsNullOrEmpty(block.Cookies)
                        || !string.IsNullOrEmpty(block.RecaptchaDataSValue);
        var cbOptional = new CheckBox
        {
            Content = "Optional Settings",
            Foreground = Brushes.White, FontSize = 12,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
            IsChecked = hasOptional,
        };
        f.Children.Add(cbOptional);

        var optionalSection = new StackPanel { Visibility = hasOptional ? Visibility.Visible : Visibility.Collapsed };
        Lbl(optionalSection, "User Agent");
        Box(optionalSection, "UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...");
        Lbl(optionalSection, "Cookies (name=value; name2=value2)");
        Box(optionalSection, "Cookies", "cookiename=value; ...");
        Lbl(optionalSection, "data-s Value (reCAPTCHA v2 one-time token)");
        Box(optionalSection, "RecaptchaDataSValue", "one-time token");
        f.Children.Add(optionalSection);

        cbOptional.Checked   += (_, _) => optionalSection.Visibility = Visibility.Visible;
        cbOptional.Unchecked += (_, _) => optionalSection.Visibility = Visibility.Collapsed;

        card.Child = f;
        outer.Children.Add(card);
        scroll.Content = outer;
        Content = scroll;

        RefreshVisibility(block.TaskType);
    }

    // ── VISIBILITY ────────────────────────────────────────────────────────────
    private void RefreshVisibility(string type)
    {
        bool isGee  = type.Equals("GeeTest",       StringComparison.OrdinalIgnoreCase);
        bool isImg  = type.Equals("ImageToText",   StringComparison.OrdinalIgnoreCase);
        bool isDd   = type.Equals("DataDome",      StringComparison.OrdinalIgnoreCase);
        bool isImp  = type.Equals("Imperva",       StringComparison.OrdinalIgnoreCase);
        bool isFun  = type.Equals("FunCaptcha",    StringComparison.OrdinalIgnoreCase);
        bool isV2   = type.StartsWith("RecaptchaV2", StringComparison.OrdinalIgnoreCase);
        bool isV3   = type.Equals("RecaptchaV3",   StringComparison.OrdinalIgnoreCase);
        bool isTurn = type.Equals("Turnstile",     StringComparison.OrdinalIgnoreCase);

        Set(targetSection,    !isGee && !isImg && !isDd && !isImp);
        Set(geeSection,       isGee);
        Set(imageSection,     isImg);
        Set(dataDomeSection,  isDd);
        Set(impervaSection,   isImp);
        Set(funCaptchaSection,isFun);
        Set(v2Section,        isV2);
        Set(v3Section,        isV3);
        Set(actionSection,    isV3 || isTurn);
        Set(dataSection,      isTurn);
    }

    private static void Set(UIElement el, bool visible)
        => el.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    // ── HELPERS ───────────────────────────────────────────────────────────────
    private static SolidColorBrush Bg(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));
    private static SolidColorBrush Br(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));

    private static Binding Bind(string path)
        => new(path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

    private static void Lbl(Panel p, string text)
        => p.Children.Add(new TextBlock
        {
            Text = text, Foreground = Br(0x9A, 0x9A, 0xAA),
            FontSize = 11, Margin = new Thickness(0, 12, 0, 4),
        });

    private static void Sep(Panel p)
        => p.Children.Add(new Rectangle
        {
            Height = 1, Fill = Br(0x2E, 0x2E, 0x38),
            Margin = new Thickness(0, 10, 0, 4),
        });

    private static TextBox MkBox(string path, string hint = "")
    {
        var tb = new TextBox
        {
            Padding = new Thickness(8, 6, 8, 6),
            Background = Bg(0x12, 0x12, 0x16), Foreground = Brushes.White,
            CaretBrush = Brushes.White, BorderBrush = Br(0x35, 0x35, 0x42),
            BorderThickness = new Thickness(1), FontSize = 12, ToolTip = hint,
        };
        tb.SetBinding(TextBox.TextProperty, Bind(path));
        return tb;
    }

    private static void Box(Panel p, string path, string hint = "")
        => p.Children.Add(MkBox(path, hint));

    // ── LOGO ──────────────────────────────────────────────────────────────────
    private static UIElement Logo()
    {
        var fill = new SolidColorBrush(Color.FromRgb(0x7D, 0x6E, 0xFF));
        var cvs  = new Canvas { Width = 216, Height = 38 };

        string[] paths = {
            // Gear 1 – large (bottom-left)
            "M2.32612 25.1962C2.51236 24.5575 2.77083 23.9417 3.09771 23.3638L2.16648 22.1891C1.86241 21.8052 1.89662 21.2577 2.2387 20.9118L3.84268 19.3075C4.18857 18.9616 4.7359 18.9312 5.11979 19.2353L6.28667 20.1591C6.88721 19.8131 7.52577 19.5433 8.19093 19.3532L8.36577 17.8516C8.42278 17.365 8.83328 17 9.3198 17H11.5889C12.0755 17 12.486 17.365 12.543 17.8516L12.7102 19.3C13.4248 19.4862 14.1089 19.7638 14.7513 20.1249L15.8802 19.2315C16.2641 18.9274 16.8114 18.9616 17.1573 19.3038L18.7613 20.908C19.1071 21.254 19.1376 21.8014 18.8335 22.1854L17.9555 23.2992C18.3204 23.9303 18.6054 24.6032 18.7955 25.3065L20.1486 25.4623C20.6352 25.5194 21 25.9299 21 26.4165V28.6861C21 29.1727 20.6351 29.5833 20.1486 29.6403L18.8145 29.7962C18.632 30.4919 18.3584 31.1571 18.0087 31.7806L18.8297 32.8184C19.1338 33.2024 19.0996 33.7498 18.7575 34.0958L17.1573 35.6962C16.8114 36.0422 16.2641 36.0726 15.8802 35.7685L14.873 34.9701C14.2192 35.3541 13.5198 35.6468 12.7863 35.8445L12.6342 37.1484C12.5772 37.635 12.1667 38 11.6802 38H9.41106C8.92454 38 8.51404 37.635 8.45703 37.1484L8.30499 35.8445C7.55241 35.643 6.83404 35.3389 6.16508 34.9397L5.11983 35.7685C4.73594 36.0726 4.1886 36.0384 3.84272 35.6962L2.23874 34.092C1.89285 33.746 1.86245 33.1986 2.16652 32.8146L3.04073 31.7084C2.69105 31.0773 2.42498 30.4044 2.25014 29.7049L0.851404 29.5453C0.364888 29.4882 0 29.0777 0 28.5911V26.3215C0 25.8349 0.364888 25.4243 0.851404 25.3673L2.32612 25.1962ZM10.5475 31.3358C12.6152 31.3358 14.299 29.6517 14.299 27.5836C14.299 25.5155 12.6152 23.8314 10.5475 23.8314C8.47983 23.8314 6.79599 25.5155 6.79599 27.5836C6.79599 29.6517 8.4798 31.3358 10.5475 31.3358Z",
            // Gear 2 – medium (middle)
            "M22.883 14.635C23.0338 14.118 23.2431 13.6194 23.5077 13.1517L22.7538 12.2007C22.5077 11.8899 22.5354 11.4467 22.8123 11.1667L24.1107 9.868C24.3907 9.58795 24.8338 9.56336 25.1446 9.80956L26.0892 10.5574C26.5754 10.2773 27.0923 10.0588 27.6307 9.90496L27.7723 8.68936C27.8184 8.29544 28.1508 8 28.5446 8H30.3815C30.7754 8 31.1077 8.29547 31.1538 8.68936L31.2892 9.86188C31.8677 10.0127 32.4215 10.2373 32.9415 10.5297L33.8554 9.80648C34.1661 9.56028 34.6092 9.58798 34.8892 9.86495L36.1877 11.1636C36.4677 11.4437 36.4923 11.8869 36.2462 12.1977L35.5354 13.0994C35.8308 13.6103 36.0615 14.155 36.2154 14.7243L37.3108 14.8505C37.7046 14.8966 38 15.229 38 15.6229V17.4602C38 17.8541 37.7046 18.1865 37.3108 18.2326L36.2308 18.3588C36.083 18.922 35.8615 19.4605 35.5785 19.9652L36.2431 20.8054C36.4893 21.1162 36.4616 21.5594 36.1846 21.8394L34.8893 23.135C34.6093 23.4151 34.1662 23.4397 33.8554 23.1935L33.04 22.5472C32.5108 22.8581 31.9446 23.095 31.3508 23.2551L31.2277 24.3106C31.1816 24.7046 30.8492 25 30.4554 25H28.6185C28.2246 25 27.8923 24.7046 27.8462 24.3106L27.7231 23.2551C27.1139 23.092 26.5323 22.8458 25.9908 22.5226L25.1446 23.1935C24.8339 23.4397 24.3908 23.412 24.1108 23.135L22.8123 21.8364C22.5323 21.5563 22.5077 21.1131 22.7538 20.8023L23.4615 19.9068C23.1785 19.3959 22.9631 18.8512 22.8215 18.2849L21.6892 18.1557C21.2954 18.1095 21 17.7772 21 17.3832V15.546C21 15.1521 21.2954 14.8197 21.6892 14.7735L22.883 14.635ZM29.5384 19.6051C31.2123 19.6051 32.5754 18.2418 32.5754 16.5677C32.5754 14.8935 31.2123 13.5302 29.5384 13.5302C27.8646 13.5302 26.5015 14.8935 26.5015 16.5677C26.5015 18.2418 27.8646 19.6051 29.5384 19.6051Z",
            // Gear 3 – small (top-left)
            "M8.55075 5.46413C8.67491 5.03835 8.84722 4.62778 9.06514 4.24255L8.44432 3.45942C8.24161 3.20345 8.26441 2.8385 8.49247 2.60787L9.56179 1.53835C9.79238 1.30772 10.1573 1.28747 10.4132 1.49022L11.1911 2.10608C11.5915 1.87543 12.0172 1.69551 12.4606 1.56879L12.5772 0.567705C12.6152 0.243302 12.8889 0 13.2132 0H14.726C15.0503 0 15.324 0.243328 15.362 0.567705L15.4735 1.53331C15.9499 1.6575 16.406 1.84251 16.8342 2.08327L17.5868 1.48769C17.8427 1.28494 18.2076 1.30775 18.4382 1.53584L19.5075 2.60536C19.7381 2.83599 19.7584 3.20094 19.5557 3.45692L18.9703 4.19949C19.2136 4.62023 19.4036 5.06879 19.5303 5.53765L20.4324 5.64156C20.7568 5.67958 21 5.95329 21 6.2777V7.79073C21 8.11513 20.7567 8.38885 20.4324 8.42687L19.543 8.53078C19.4213 8.99457 19.2389 9.43809 19.0058 9.85373L19.5531 10.5456C19.7559 10.8016 19.7331 11.1665 19.505 11.3972L18.4382 12.4642C18.2076 12.6948 17.8427 12.7151 17.5868 12.5123L16.9153 11.9801C16.4795 12.2361 16.0132 12.4312 15.5242 12.563L15.4228 13.4323C15.3848 13.7567 15.1111 14 14.7868 14H13.274C12.9497 14 12.676 13.7567 12.638 13.4323L12.5367 12.563C12.0349 12.4287 11.556 12.2259 11.1101 11.9598L10.4132 12.5123C10.1573 12.7151 9.7924 12.6923 9.56181 12.4642L8.49249 11.3946C8.2619 11.164 8.24163 10.7991 8.44435 10.5431L9.02715 9.80558C8.79403 9.38487 8.61665 8.93628 8.50009 8.46995L7.5676 8.3635C7.24326 8.32549 7 8.05177 7 7.72737V6.21434C7 5.88993 7.24326 5.61622 7.5676 5.57821L8.55075 5.46413ZM14.0317 9.55718C15.4101 9.55718 16.5327 8.43444 16.5327 7.05573C16.5327 5.67702 15.4101 4.55428 14.0317 4.55428C12.6532 4.55428 11.5307 5.67702 11.5307 7.05573C11.5307 8.43444 12.6532 9.55718 14.0317 9.55718Z",
            // Letter C
            "M59.7937 22.6049C58.8561 23.3746 57.1941 23.7787 55.4895 23.7787C50.9935 23.7787 48 21.4118 48 16.3509C48 11.2899 50.9935 9 55.4895 9C57.1941 9 58.8561 9.40411 59.7937 10.1738C59.3249 11.2899 58.1743 11.9634 58.1743 11.9634C58.1743 11.9634 57.0876 11.2514 55.4895 11.2514C53.1243 11.2514 50.8552 12.5985 50.8552 16.3509C50.8552 20.1033 53.1243 21.5273 55.4895 21.5273C57.0663 21.5273 58.1743 20.8153 58.1743 20.8153C58.1743 20.8153 59.3249 21.4888 59.7937 22.6049Z",
            // Letter a
            "M64.9491 23.6926C62.2391 23.6926 61.2002 22.6542 61.2002 20.7503C61.2002 18.8658 62.2616 17.8081 65.1524 17.8081H67.2301V17.3081C67.2301 16.212 66.3945 15.9812 65.2879 15.9812C63.7974 15.9812 62.3068 16.3658 62.3068 16.3658C62.3068 16.3658 61.9681 15.2889 62.1036 14.2889C62.1036 14.2889 63.2328 13.9043 65.2879 13.9043C68.4271 13.9043 70.008 14.7312 70.008 17.1927V23.6157C70.008 23.6157 67.4334 23.6926 64.9491 23.6926ZM65.175 21.8657C66.0784 21.8657 67.2301 21.808 67.2301 21.808V19.5773L65.3782 19.5581C64.0458 19.5388 63.9103 20.1542 63.9103 20.7503C63.9103 21.3657 64.1135 21.8657 65.175 21.8657Z",
            // Letter p
            "M72.6133 26.9428V14.0584C72.6133 14.0584 74.7588 13.9238 77.3559 13.9238C80.6984 13.9238 82.347 15.1353 82.347 18.6738C82.347 21.616 80.6984 23.6352 76.9268 23.6352C76.4074 23.6352 75.8202 23.616 75.3685 23.5968V26.9428C74.9394 26.9813 74.4878 27.0005 74.0812 27.0005C73.2682 27.0005 72.6133 26.9428 72.6133 26.9428ZM75.3685 16.0584V21.6737H76.9268C78.9594 21.6737 79.5014 20.3083 79.5014 18.6738C79.5014 16.7315 78.9594 16.0584 77.3559 16.0584H75.3685Z",
            // Letter M
            "M91.9787 15.019H92.0691C92.0691 15.019 92.1368 14.6921 94.1242 11.6152L95.7051 9.17293C96.2697 9.13446 97.0827 9.11523 97.6248 9.11523C98.7088 9.11523 99.6347 9.17293 99.6347 9.17293V23.615C99.1379 23.6535 98.6636 23.6727 98.2345 23.6727C97.3763 23.6727 96.7214 23.615 96.7214 23.615L96.7666 12.1536H96.6762C96.6762 12.1536 96.4052 13.019 94.4856 15.8267L92.1594 19.2305H91.8884L89.6074 15.8844C87.7329 13.1344 87.3716 12.1536 87.3716 12.1536H87.2813L87.3264 23.615C87.3264 23.615 86.6715 23.6727 85.8133 23.6727C85.3842 23.6727 84.9099 23.6535 84.4131 23.615V9.17293C85.0003 9.13446 85.723 9.11523 86.265 9.11523C87.349 9.11523 88.3427 9.17293 88.3427 9.17293L90.1269 11.9037C91.8884 14.6151 91.9787 15.019 91.9787 15.019Z",
            // Letter o
            "M102.019 18.5965C102.019 15.635 103.035 13.9043 106.875 13.9043C110.714 13.9043 111.753 15.635 111.753 18.5965C111.753 21.9234 110.262 23.7311 106.875 23.7311C103.487 23.7311 102.019 21.9234 102.019 18.5965ZM104.752 18.6542C104.752 20.4619 105.294 21.6734 106.875 21.6734C108.455 21.6734 109.02 20.558 109.02 18.7311C109.02 16.9427 108.455 15.9812 106.875 15.9812C105.294 15.9812 104.752 16.8466 104.752 18.6542Z",
            // Letter n
            "M117.989 16.0584H116.589V23.616C116.589 23.616 115.979 23.6737 115.166 23.6737C114.76 23.6737 114.285 23.6544 113.811 23.616V14.0584C113.811 14.0584 115.731 13.9238 117.989 13.9238C121.58 13.9238 122.709 15.0007 122.709 18.0007V23.616C122.709 23.616 122.099 23.6737 121.286 23.6737C120.88 23.6737 120.406 23.6544 119.931 23.616V17.8276C119.931 16.443 119.457 16.0584 117.989 16.0584Z",
            // Letter s
            "M128.967 13.9043C131.496 13.9043 132.783 14.8274 132.783 14.8274C132.783 14.8274 132.354 15.7697 131.315 16.4043C131.315 16.4043 130.367 15.8658 128.967 15.8658C127.973 15.8658 127.544 16.2504 127.544 16.8273C127.544 17.7696 128.876 17.7504 130.322 17.9235C131.767 18.0965 133.212 18.885 133.212 20.635C133.212 22.7695 131.722 23.7311 128.515 23.7311C125.737 23.7311 124.63 22.7311 124.63 22.7311C124.63 22.7311 125.059 21.808 126.098 21.2119C126.098 21.2119 127.137 21.7696 128.515 21.7696C129.847 21.7696 130.525 21.4042 130.525 20.7119C130.525 19.6542 129.079 19.6927 127.589 19.5581C126.189 19.4234 124.834 18.6542 124.834 16.9042C124.834 14.7889 126.234 13.9043 128.967 13.9043Z",
            // Letter t
            "M135.448 23.615V11.9228L138.203 11.4229V13.9805H141.094C141.139 14.3074 141.162 14.6536 141.162 14.9613C141.162 15.5766 141.094 16.0574 141.094 16.0574H138.203V23.615C138.203 23.615 137.593 23.6727 136.78 23.6727C136.374 23.6727 135.922 23.6534 135.448 23.615Z",
            // Letter e
            "M151.251 19.6734H144.34C144.34 21.135 145.424 21.6926 146.983 21.6926C148.88 21.6926 150.144 21.1734 150.144 21.1734C150.709 22.1542 150.845 23.1734 150.845 23.1734C150.845 23.1734 149.444 23.7503 146.983 23.7503C143.121 23.7503 141.562 22.0965 141.562 18.7696C141.562 15.5197 143.369 13.9043 146.576 13.9043C149.58 13.9043 151.341 15.712 151.341 18.5388C151.341 19.2311 151.251 19.6734 151.251 19.6734ZM146.576 15.9427C145.04 15.9427 144.408 16.7312 144.386 17.8465H148.428V17.5581C148.428 16.8081 147.931 15.9427 146.576 15.9427Z",
            // Letter r
            "M156.29 13.9238C157.916 13.9238 159.204 13.9815 159.204 13.9815C159.204 13.9815 159.271 14.4431 159.271 15.0584C159.271 15.3661 159.249 15.7123 159.204 16.0584H156.177V23.616C156.177 23.616 155.545 23.6737 154.732 23.6737C154.325 23.6737 153.851 23.6544 153.399 23.616V14.0584C153.399 14.0584 154.529 13.9238 156.29 13.9238Z",
            // Second C (Cloud C)
            "M177.566 22.6049C176.629 23.3746 174.967 23.7787 173.262 23.7787C168.766 23.7787 165.772 21.4118 165.772 16.3509C165.772 11.2899 168.766 9 173.262 9C174.967 9 176.629 9.40411 177.566 10.1738C177.097 11.2899 175.947 11.9634 175.947 11.9634C175.947 11.9634 174.86 11.2514 173.262 11.2514C170.897 11.2514 168.628 12.5985 168.628 16.3509C168.628 20.1033 170.897 21.5273 173.262 21.5273C174.839 21.5273 175.947 20.8153 175.947 20.8153C175.947 20.8153 177.097 21.4888 177.566 22.6049Z",
            // Letter l
            "M179 23.6159V9.67382L181.777 9.17383V23.6159C181.326 23.6544 180.829 23.6736 180.422 23.6736C179.609 23.6736 179 23.6159 179 23.6159Z",
            // Letter o2
            "M183.814 18.5965C183.814 15.635 184.831 13.9043 188.67 13.9043C192.509 13.9043 193.548 15.635 193.548 18.5965C193.548 21.9234 192.058 23.7311 188.67 23.7311C185.282 23.7311 183.814 21.9234 183.814 18.5965ZM186.547 18.6542C186.547 20.4619 187.089 21.6734 188.67 21.6734C190.251 21.6734 190.816 20.558 190.816 18.7311C190.816 16.9427 190.251 15.9812 188.67 15.9812C187.089 15.9812 186.547 16.8466 186.547 18.6542Z",
            // Letter u
            "M204.506 13.9815V23.6059C204.506 23.6059 202.947 23.7314 200.056 23.7314C197.166 23.7314 195.607 23.3083 195.607 20.866V13.9815C196.082 13.9431 196.556 13.9238 196.962 13.9238C197.775 13.9238 198.385 13.9815 198.385 13.9815V20.5006C198.385 21.366 198.995 21.6545 200.056 21.6545C201.118 21.6545 201.728 21.6545 201.728 21.6545V13.9815C202.202 13.9431 202.654 13.9238 203.06 13.9238C203.873 13.9238 204.506 13.9815 204.506 13.9815Z",
            // Letter d
            "M211.032 23.6919C207.689 23.6919 206.289 22.1919 206.289 18.6728C206.289 15.9228 207.915 13.9229 211.235 13.9229C211.777 13.9229 213.222 13.9805 213.222 13.9805V9.17293C213.697 9.13446 214.171 9.11523 214.577 9.11523C215.39 9.11523 216 9.17293 216 9.17293V23.615C216 23.615 213.313 23.6919 211.032 23.6919ZM211.258 21.6727H213.222V16.0382H211.461C209.857 16.0382 209.135 17.2113 209.135 18.6728C209.135 20.6535 209.677 21.6727 211.258 21.6727Z",
        };

        foreach (var d in paths)
            cvs.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(d), Fill = fill });

        var vb = new Viewbox
        {
            Child = cvs,
            Width  = 200,
            Height = 35,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        return vb;
    }
}
