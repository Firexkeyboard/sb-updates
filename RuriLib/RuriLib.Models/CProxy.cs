using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using LiteDB;

namespace RuriLib.Models;

public class CProxy : Persistable<Guid>
{
    private string proxy = "";
    private string username = "";
    private string password = "";
    private ProxyType type;
    private string country = "";
    private int ping;
    private DateTime lastUsed = new DateTime(1970, 1, 1);
    private DateTime lastChecked = new DateTime(1970, 1, 1);
    private ProxyWorking working = ProxyWorking.UNTESTED;
    private bool disabled = false;

    public string Proxy
    {
        get => proxy;
        set { proxy = value; OnPropertyChanged("Proxy"); }
    }

    public string Username
    {
        get => username;
        set { username = value; OnPropertyChanged("Username"); }
    }

    public string Password
    {
        get => password;
        set { password = value; OnPropertyChanged("Password"); }
    }

    public ProxyType Type
    {
        get => type;
        set { type = value; OnPropertyChanged("Type"); }
    }

    public string Country
    {
        get => country;
        set { country = value; OnPropertyChanged("Country"); }
    }

    public int Ping
    {
        get => ping;
        set { ping = value; OnPropertyChanged("Ping"); }
    }

    public CProxy Next { get; set; }

    public DateTime LastUsed
    {
        get => lastUsed;
        set { lastUsed = value; OnPropertyChanged("LastUsed"); }
    }

    public DateTime LastChecked
    {
        get => lastChecked;
        set { lastChecked = value; OnPropertyChanged("LastChecked"); }
    }

    public ProxyWorking Working
    {
        get => working;
        set { working = value; OnPropertyChanged("Working"); }
    }

    public bool Disabled
    {
        get => disabled;
        set { disabled = value; OnPropertyChanged("Disabled"); }
    }

    [BsonIgnore] public bool HasNext => Next != null;
    [BsonIgnore] public int Uses { get; set; }
    [BsonIgnore] public int Hooked { get; set; }
    [BsonIgnore] public string Clearance { get; set; } = "";
    [BsonIgnore] public string Cfduid { get; set; } = "";
    [BsonIgnore] public Status Status { get; set; }

    [BsonIgnore]
    public string Host
    {
        get
        {
            try { return Proxy.Split(':').First(); }
            catch { return ""; }
        }
    }

    [BsonIgnore]
    public string Port
    {
        get
        {
            try { return Proxy.Split(':').Last(); }
            catch { return ""; }
        }
    }

    [BsonIgnore]
    public bool IsValidNumeric
    {
        get
        {
            if (!(Host == string.Empty) && !(Port == string.Empty) &&
                !Port.Any(c => !char.IsDigit(c)) && Host.Split('.').Count() == 4)
                return IsNumeric;
            return false;
        }
    }

    [BsonIgnore] public bool IsNumeric => !Host.Split('.').Any(x => x.Any(c => !char.IsDigit(c)));
    [BsonIgnore] public TimeSpan UsedAgo => DateTime.Now - LastUsed;

    public CProxy() { }

    public CProxy(string proxy, ProxyType type)
    {
        Proxy = proxy;
        Type = type;
    }

    public CProxy Parse(string proxy, ProxyType defaultType = ProxyType.Http,
        string defaultUsername = "", string defaultPassword = "")
    {
        // Handle chain proxies: proxy1->proxy2
        string[] parts = proxy.Split(new string[] { "->" }, 2, StringSplitOptions.None);
        string text = parts[0].Trim();

        // Handle type prefix: (http)host:port
        if (text.StartsWith("("))
        {
            var groups = Regex.Match(text, "^\\((.*)\\)(.*)").Groups;
            Enum.TryParse<ProxyType>(groups[1].Value, ignoreCase: true, out var result);
            Type = result;
            text = text.Replace("(" + groups[1].Value + ")", "").Trim();
        }
        else
        {
            Type = defaultType;
        }

        string host, port, user, pass;

        if (text.Contains("://"))
        {
            // Format: protocol://user:pass@host:port  OR  protocol://host:port
            try
            {
                var uri = new Uri(text);
                if (Enum.TryParse<ProxyType>(uri.Scheme, ignoreCase: true, out var protoType))
                    Type = protoType;
                host = uri.Host;
                port = uri.Port > 0 ? uri.Port.ToString() : "80";
                if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(":"))
                {
                    int colonIdx = uri.UserInfo.IndexOf(':');
                    user = uri.UserInfo.Substring(0, colonIdx);
                    pass = uri.UserInfo.Substring(colonIdx + 1);
                }
                else
                {
                    user = uri.UserInfo;
                    pass = defaultPassword;
                }
            }
            catch
            {
                host = text; port = "80"; user = defaultUsername; pass = defaultPassword;
            }
        }
        else if (text.Contains("@"))
        {
            // Formats with @:
            //   host:port@user:pass
            //   user:pass@host:port
            int atIdx = text.LastIndexOf('@');
            string left  = text.Substring(0, atIdx);
            string right = text.Substring(atIdx + 1);

            string rightLastPart = right.Contains(":") ? right.Split(':').Last() : "";
            string leftLastPart  = left.Contains(":")  ? left.Split(':').Last()  : "";

            if (int.TryParse(rightLastPart, out _))
            {
                // user:pass@host:port
                int c = right.IndexOf(':');
                host = right.Substring(0, c);
                port = right.Substring(c + 1);
                int lc = left.IndexOf(':');
                user = lc >= 0 ? left.Substring(0, lc)  : left;
                pass = lc >= 0 ? left.Substring(lc + 1) : defaultPassword;
            }
            else if (int.TryParse(leftLastPart, out _))
            {
                // host:port@user:pass
                int c = left.IndexOf(':');
                host = left.Substring(0, c);
                port = left.Substring(c + 1);
                int rc = right.IndexOf(':');
                user = rc >= 0 ? right.Substring(0, rc)  : right;
                pass = rc >= 0 ? right.Substring(rc + 1) : defaultPassword;
            }
            else
            {
                // Fallback: treat right as host:port, left as user
                int c = right.IndexOf(':');
                host = c >= 0 ? right.Substring(0, c) : right;
                port = c >= 0 ? right.Substring(c + 1) : "80";
                user = left;
                pass = defaultPassword;
            }
        }
        else if (text.Contains("/") && !text.Contains("://"))
        {
            // Format: host:port/user/pass  (slash-separated credentials)
            int slashIdx = text.IndexOf('/');
            string hostPort = text.Substring(0, slashIdx);
            string creds    = text.Substring(slashIdx + 1);
            int hc = hostPort.IndexOf(':');
            host = hc >= 0 ? hostPort.Substring(0, hc) : hostPort;
            port = hc >= 0 ? hostPort.Substring(hc + 1) : "80";
            int sc = creds.IndexOf('/');
            user = sc >= 0 ? creds.Substring(0, sc)  : creds;
            pass = sc >= 0 ? creds.Substring(sc + 1) : defaultPassword;
        }
        else
        {
            // No @ — colon-separated only
            // Formats: host:port  |  host:port:user:pass[:type]  |  user:pass:host:port[:type]
            string[] f = text.Split(':');

            // Strip trailing type field if last token is a known ProxyType name
            if (f.Length > 0 && Enum.TryParse<ProxyType>(f[f.Length - 1], ignoreCase: true, out var trailingType))
            {
                Type = trailingType;
                f = f.Take(f.Length - 1).ToArray();
            }

            if (f.Length == 1)
            {
                // Only host, no port — assume port 80
                host = f[0]; port = "80";
                user = defaultUsername; pass = defaultPassword;
            }
            else if (f.Length == 2)
            {
                host = f[0]; port = f[1];
                user = defaultUsername; pass = defaultPassword;
            }
            else if (f.Length >= 4)
            {
                if (int.TryParse(f[1], out _))
                {
                    // host:port:user:pass
                    host = f[0]; port = f[1];
                    user = f[2];
                    pass = string.Join(":", f.Skip(3));
                }
                else if (int.TryParse(f[3], out _))
                {
                    // user:pass:host:port
                    host = f[2]; port = f[3];
                    user = f[0]; pass = f[1];
                }
                else
                {
                    // Default: host:port:user:pass
                    host = f[0]; port = f[1];
                    user = f[2]; pass = f[3];
                }
            }
            else if (f.Length == 3)
            {
                host = f[0]; port = f[1]; user = f[2]; pass = defaultPassword;
            }
            else
            {
                host = text; port = "80";
                user = defaultUsername; pass = defaultPassword;
            }
        }

        Proxy    = $"{host}:{port}";
        Username = !string.IsNullOrEmpty(user) ? user : defaultUsername;
        Password = !string.IsNullOrEmpty(pass) ? pass : defaultPassword;

        if (parts.Length > 1)
            Next = new CProxy().Parse(parts[1], defaultType, defaultUsername, defaultPassword);

        return this;
    }

    /// <summary>
    /// Returns an IWebProxy configured for this proxy.
    /// .NET 8 SocketsHttpHandler natively supports socks4://, socks4a://, socks5://.
    /// Chain proxies (proxy1->proxy2) are not natively supported; the last hop is used.
    /// </summary>
    public IWebProxy GetWebProxy()
    {
        // For chains, find the outermost (last) hop .NET will actually route through
        CProxy last = this;
        while (last.Next != null) last = last.Next;
        return last.BuildWebProxy();
    }

    private IWebProxy BuildWebProxy()
    {
        if (string.IsNullOrWhiteSpace(Proxy)) return null;
        string scheme = Type switch
        {
            ProxyType.Http    => "http",
            ProxyType.Socks4  => "socks4",
            ProxyType.Socks4a => "socks4a",
            ProxyType.Socks5  => "socks5",
            _                 => "http"
        };

        var wp = new WebProxy($"{scheme}://{Proxy}");
        if (!string.IsNullOrEmpty(Username))
            wp.Credentials = new NetworkCredential(Username, Password);
        return wp;
    }

    internal string GetCustomProxy()
    {
        if (string.IsNullOrEmpty(Username))
            return Proxy;
        if (BlockFunction.CountStringOccurrences(Proxy, ":") == 1)
            return Username + ":" + Password + "@" + Proxy;
        return Proxy;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Username))
            return Proxy;
        if (BlockFunction.CountStringOccurrences(Proxy, ":") == 1)
            return Proxy + ":" + Username + ":" + Password;
        return Proxy;
    }
}
