using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

// ================================================================
// EO.Base shim — replaces EO.Base.dll
// ================================================================
namespace EO.Base
{
    public static class Runtime
    {
        public static void Shutdown() { }
    }
}

// ================================================================
// EO.WebBrowser shim — replaces EO.WebBrowser.dll
// ================================================================
namespace EO.WebBrowser
{
    public class Cookie
    {
        public string Name { get; }
        public string Value { get; }
        public string Domain { get; set; }
        public string Path { get; set; }

        public Cookie(string name, string value)
        {
            Name = name;
            Value = value;
            Path = "/";
        }
    }

    public class BeforeNavigateEventArgs : CancelEventArgs
    {
        public BeforeNavigateEventArgs() : base(false) { }
    }

    public class LoadCompletedEventArgs : EventArgs
    {
        public int HttpStatusCode { get; }
        public LoadCompletedEventArgs(int statusCode) => HttpStatusCode = statusCode;
    }

    public delegate void BeforeNavigateHandler(object sender, BeforeNavigateEventArgs e);
    public delegate void LoadCompletedEventHandler(object sender, LoadCompletedEventArgs e);

    public class CompatCookieManager
    {
        private readonly WebView2 _wv2;
        internal CompatCookieManager(WebView2 wv2) => _wv2 = wv2;

        public void SetCookie(string url, Cookie cookie)
        {
            _ = SetCookieAsync(cookie);
        }

        private async Task SetCookieAsync(Cookie cookie)
        {
            try
            {
                if (_wv2.CoreWebView2 == null)
                    await _wv2.EnsureCoreWebView2Async();
                var wv2Cookie = _wv2.CoreWebView2.CookieManager.CreateCookie(
                    cookie.Name, cookie.Value, cookie.Domain ?? "", cookie.Path ?? "/");
                _wv2.CoreWebView2.CookieManager.AddOrUpdateCookie(wv2Cookie);
            }
            catch { }
        }
    }

    public class CompatEngine
    {
        public CompatCookieManager CookieManager { get; }
        internal CompatEngine(WebView2 wv2) => CookieManager = new CompatCookieManager(wv2);
    }
}

// ================================================================
// EO.WebEngine shim — replaces EO.WebEngine.dll
// ================================================================
namespace EO.WebEngine
{
    public class WebViewOptions
    {
        public bool EnableWebSecurity { get; set; } = true;
        public bool AllowJavaScriptCloseWindow { get; set; } = true;
        public bool AllowJavaScriptAccessClipboard { get; set; } = false;
        public bool AllowZooming { get; set; } = true;
    }

    public class BrowserOptions : WebViewOptions { }

    public class EngineOptions
    {
        public static readonly EngineOptions Default = new EngineOptions();
        private EngineOptions() { }

        public void SetDefaultBrowserOptions(WebViewOptions options) { }
        public void SetDefaultWebViewOptions(WebViewOptions options) { }
    }
}

// ================================================================
// EO.Wpf shim — replaces EO.WebBrowser.Wpf.dll (WPF controls)
// ================================================================
namespace EO.Wpf
{
    using EO.WebBrowser;

    public class WebView : WebView2
    {
        private string _pendingHtml;
        private string _pendingUrl;
        private readonly CompatEngine _engine;

        public event BeforeNavigateHandler BeforeNavigate;
        public event LoadCompletedEventHandler LoadCompleted;

        public CompatEngine Engine => _engine;

        public WebView()
        {
            _engine = new CompatEngine(this);

            NavigationStarting += (object s, CoreWebView2NavigationStartingEventArgs e) =>
            {
                var args = new BeforeNavigateEventArgs();
                BeforeNavigate?.Invoke(this, args);
                if (args.Cancel) e.Cancel = true;
            };

            NavigationCompleted += (object s, CoreWebView2NavigationCompletedEventArgs e) =>
            {
                LoadCompleted?.Invoke(this, new LoadCompletedEventArgs(e.HttpStatusCode));
            };

            CoreWebView2InitializationCompleted += (object s, CoreWebView2InitializationCompletedEventArgs e) =>
            {
                if (!e.IsSuccess) return;
                if (_pendingHtml != null)
                {
                    CoreWebView2.NavigateToString(_pendingHtml);
                    _pendingHtml = null;
                }
                else if (_pendingUrl != null)
                {
                    CoreWebView2.Navigate(_pendingUrl);
                    _pendingUrl = null;
                }
            };

            Loaded += async (object s, RoutedEventArgs e) =>
            {
                try { await EnsureCoreWebView2Async(); }
                catch { }
            };
        }

        public void LoadHtml(string html)
        {
            if (CoreWebView2 != null)
                CoreWebView2.NavigateToString(html);
            else
                _pendingHtml = html;
        }

        public void LoadHtml(string html, string baseUrl)
        {
            LoadHtml(html);
        }

        public void LoadUrl(string url)
        {
            try { Source = new Uri(url); }
            catch { }
        }
    }

    public class WebControl : ContentControl
    {
        public static readonly DependencyProperty WebViewProperty =
            DependencyProperty.Register("WebView", typeof(object), typeof(WebControl),
                new PropertyMetadata(null));

        public object WebView
        {
            get => GetValue(WebViewProperty);
            set => SetValue(WebViewProperty, value);
        }
    }

    public class WebViewHost : ContentControl { }
}
