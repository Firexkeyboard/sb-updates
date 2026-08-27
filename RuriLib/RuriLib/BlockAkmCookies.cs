using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using RuriLib.LS;
using RuriLib.Models;
using Diag = System.Diagnostics;

namespace RuriLib;

public class BlockAkmCookies : BlockBase
{
    // ── Shared client — created once, reused across every bot/request ─────────
    // HttpClient is thread-safe and designed to be reused. Creating a new one
    // per request exhausts sockets under high concurrency (TIME_WAIT).
    private static readonly HttpClient _http = new HttpClient
    {
        Timeout = Timeout.InfiniteTimeSpan   // per-request CTS handles timeout
    };

    private static Diag.Process _akmProcess;
    private static readonly object _procLock = new object();

    static BlockAkmCookies()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { _akmProcess?.Kill(); } catch { }
        };
    }

    // ── Properties ────────────────────────────────────────────────────────────
    private string url             = "";
    private string userAgent       = "";
    private string proxy           = "";
    private int    port            = 8085;
    private string outputCookies   = "AKM_COOKIES";
    private string outputUserAgent = "AKM_UA";

    public string Url             { get => url;             set { url = value;             OnPropertyChanged("Url"); } }
    public string UserAgent       { get => userAgent;       set { userAgent = value;       OnPropertyChanged("UserAgent"); } }
    public string Proxy           { get => proxy;           set { proxy = value;           OnPropertyChanged("Proxy"); } }
    public int    Port            { get => port;            set { port = value;            OnPropertyChanged("Port"); } }
    public string OutputCookies   { get => outputCookies;   set { outputCookies = value;   OnPropertyChanged("OutputCookies"); } }
    public string OutputUserAgent { get => outputUserAgent; set { outputUserAgent = value; OnPropertyChanged("OutputUserAgent"); } }

    public BlockAkmCookies() { Label = "AKM-COOKIES"; }

    // ── Serialization ─────────────────────────────────────────────────────────
    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        Url = LineParser.ParseLiteral(ref input, "Url");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputCookies = LineParser.ParseLiteral(ref input, "OutputCookies");

        while (input != "")
        {
            string tok = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
            if      (tok == "UA")    { LineParser.ParseToken(ref input, TokenType.Parameter, true); OutputUserAgent = LineParser.ParseLiteral(ref input, "OutputUserAgent"); }
            else if (tok == "PROXY") { LineParser.ParseToken(ref input, TokenType.Parameter, true); Proxy           = LineParser.ParseLiteral(ref input, "Proxy"); }
            else if (tok == "PORT")  { LineParser.ParseToken(ref input, TokenType.Parameter, true); Port            = int.Parse(LineParser.ParseLiteral(ref input, "Port")); }
            else break;
        }
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("AKM-COOKIES").Literal(Url).Literal(OutputCookies);
        if (OutputUserAgent != "AKM_UA")    bw.Token("UA").Literal(OutputUserAgent);
        if (!string.IsNullOrEmpty(Proxy))   bw.Token("PROXY").Literal(Proxy);
        if (Port != 8085)                   bw.Token("PORT").Literal(Port.ToString());
        return bw.ToString();
    }

    // ── Main execution ────────────────────────────────────────────────────────
    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl   = ReplaceValues(url,       data);
        string resolvedProxy = ReplaceValues(proxy,     data);
        string resolvedUa    = ReplaceValues(userAgent, data);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            throw new Exception($"[AKM] Invalid URL '{resolvedUrl}' — must start with http:// or https://");

        // Ensure server is alive, restart automatically if it crashed
        EnsureServerRunning(data, port);

        var sw = Diag.Stopwatch.StartNew();

        data.Log(new LogEntry("Akamai Cookie Solver", Colors.Teal));
        data.Log(new LogEntry($"{"URL",-14}: {resolvedUrl}", Colors.White));
        if (!string.IsNullOrEmpty(resolvedProxy))
            data.Log(new LogEntry($"{"Proxy",-14}: {resolvedProxy}", Colors.White));

        var payload = new JObject
        {
            ["url"]       = resolvedUrl,
            ["userAgent"] = resolvedUa,
            ["proxy"]     = resolvedProxy,
        };

        // Per-request 30 s hard limit — won't block a bot thread indefinitely
        using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var       content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = _http.PostAsync($"http://localhost:{port}/akmcookies", content, cts.Token)
                            .GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            throw new Exception($"[AKM] Request timed out after 30 s (port {port}).");
        }
        catch (Exception ex)
        {
            // Connection failed — mark process as dead so next call restarts it
            lock (_procLock) { try { _akmProcess?.Kill(); } catch { } _akmProcess = null; }
            throw new Exception($"[AKM] Cannot reach server on port {port}: {ex.Message}");
        }

        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        JObject json;
        try   { json = JObject.Parse(body); }
        catch { throw new Exception($"[AKM] Bad JSON response: {body}"); }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[AKM] Server error {(int)response.StatusCode}: {json["error"] ?? body}");

        string cookies  = json["cookies"]?.ToString();
        string returnUa = json["userAgent"]?.ToString();

        if (string.IsNullOrEmpty(cookies))
            throw new Exception($"[AKM] No cookies in response: {body}");

        sw.Stop();
        data.Log(new LogEntry($"{"Elapsed",-14}: {sw.ElapsedMilliseconds} ms", Colors.White));

        var responseJson = new JObject
        {
            ["solution"]         = new JObject { ["cookies"] = cookies, ["userAgent"] = returnUa },
            ["status"]           = "ready",
            ["errorId"]          = 0,
            ["errorCode"]        = null,
            ["errorDescription"] = null,
        };
        string responseJsonStr = responseJson.ToString(Newtonsoft.Json.Formatting.None);
        data.Log(new LogEntry(responseJsonStr, Colors.GreenYellow));
        data.ResponseSource = responseJsonStr;

        string cookiesVar = ReplaceValues(outputCookies,   data);
        string uaVar      = ReplaceValues(outputUserAgent, data);

        data.Variables.Set(new CVar(cookiesVar, cookies));
        if (!string.IsNullOrEmpty(returnUa))
            data.Variables.Set(new CVar(uaVar, returnUa));

        data.Log(new LogEntry($"Saved cookies to <{cookiesVar}>, UA to <{uaVar}>", Colors.GreenYellow));
    }

    // ── Server lifecycle ──────────────────────────────────────────────────────
    private static void EnsureServerRunning(BotData data, int port)
    {
        // Fast path: no lock needed if port is open and process is alive
        if (IsPortOpen(port) && ProcessAlive())
            return;

        lock (_procLock)
        {
            // Double-checked inside lock
            if (IsPortOpen(port) && ProcessAlive())
                return;

            // Kill stale / crashed process before restarting
            if (_akmProcess != null)
            {
                try { if (!_akmProcess.HasExited) _akmProcess.Kill(); } catch { }
                _akmProcess = null;
            }

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe    = Path.Combine(exeDir, "akmcookies.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"[AKM] akmcookies.exe not found at: {exe}");

            data.Log(new LogEntry("[AKM] Starting akmcookies.exe…", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(exe, port.ToString())
            {
                WorkingDirectory       = exeDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false,
            };
            _akmProcess = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_akmProcess);

            // Poll up to 30 s (300 × 100 ms)
            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;
                if (_akmProcess.HasExited)
                    throw new Exception("[AKM] akmcookies.exe exited unexpectedly during startup.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[AKM] akmcookies.exe did not open port {port} within 30 s.");

            data.Log(new LogEntry("[AKM] Server ready.", Colors.GreenYellow));
        }
    }

    private static bool ProcessAlive()
    {
        try { return _akmProcess != null && !_akmProcess.HasExited; }
        catch { return false; }
    }

    // Try 3 times with 400 ms timeout each — avoids false negatives under load
    private static bool IsPortOpen(int port)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var tcp = new TcpClient();
                var ar = tcp.BeginConnect("127.0.0.1", port, null, null);
                bool connected = ar.AsyncWaitHandle.WaitOne(400);
                if (connected)
                {
                    try { tcp.EndConnect(ar); } catch { }
                    return true;
                }
            }
            catch { }

            if (attempt < 2) Thread.Sleep(50);
        }
        return false;
    }
}
