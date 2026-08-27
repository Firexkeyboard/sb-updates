using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using RuriLib.LS;
using RuriLib.Models;
using Diag = System.Diagnostics;

namespace RuriLib;

public class BlockTurnstile : BlockBase
{
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const int SW_MINIMIZE = 6;

    private static Diag.Process _cfProcess;
    private static readonly object _procLock = new object();

    static BlockTurnstile()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { _cfProcess?.Kill(); } catch { }
        };
    }

    private string domain = "";
    private string siteKey = "";
    private string action = "";
    private string proxy = "";
    private int port = 8742;
    private string outputVariable = "TURNSTILE_TOKEN";

    public string Domain
    {
        get => domain;
        set { domain = value; OnPropertyChanged("Domain"); }
    }

    public string SiteKey
    {
        get => siteKey;
        set { siteKey = value; OnPropertyChanged("SiteKey"); }
    }

    public string Action
    {
        get => action;
        set { action = value; OnPropertyChanged("Action"); }
    }

    public string Proxy
    {
        get => proxy;
        set { proxy = value; OnPropertyChanged("Proxy"); }
    }

    public int Port
    {
        get => port;
        set { port = value; OnPropertyChanged("Port"); }
    }

    public string OutputVariable
    {
        get => outputVariable;
        set { outputVariable = value; OnPropertyChanged("OutputVariable"); }
    }

    public BlockTurnstile()
    {
        Label = "TURNSTILE";
    }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#"))
            Label = LineParser.ParseLabel(ref input);

        Domain = LineParser.ParseLiteral(ref input, "Domain");
        SiteKey = LineParser.ParseLiteral(ref input, "SiteKey");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");

        while (input != "")
        {
            string tok = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
            if (tok == "PORT")
            {
                LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
                Port = int.Parse(LineParser.ParseLiteral(ref input, "Port"));
            }
            else if (tok == "ACTION")
            {
                LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
                Action = LineParser.ParseLiteral(ref input, "Action");
            }
            else if (tok == "PROXY")
            {
                LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
                Proxy = LineParser.ParseLiteral(ref input, "Proxy");
            }
            else break;
        }

        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("TURNSTILE").Literal(Domain).Literal(SiteKey).Literal(OutputVariable);
        if (!string.IsNullOrEmpty(Action))
            bw.Token("ACTION").Literal(Action);
        if (!string.IsNullOrEmpty(Proxy))
            bw.Token("PROXY").Literal(Proxy);
        if (Port != 8742)
            bw.Token("PORT").Literal(Port.ToString());
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedDomain  = ReplaceValues(domain, data);
        string resolvedSiteKey = ReplaceValues(siteKey, data);
        string resolvedAction  = ReplaceValues(action, data);

        // Only send proxy to cf-bypass if the user explicitly set one in the block field.
        // Auto-detection from data.Proxy is intentionally omitted: the bot proxy is often
        // an API credential (e.g. DataImpulse format), not a real HTTP proxy, and sending
        // it to cf-bypass breaks the Turnstile solve.
        string resolvedProxy = ReplaceValues(proxy, data);

        EnsureCfBypassRunning(data, port);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // ── Request info ─────────────────────────────────────────────────────
        data.Log(new LogEntry("Cloudflare Turnstile Solver", Colors.Cyan));
        data.Log(new LogEntry($"{"Domain",-14}: {resolvedDomain}",  Colors.White));
        data.Log(new LogEntry($"{"SiteKey",-14}: {resolvedSiteKey}", Colors.White));
        if (!string.IsNullOrEmpty(resolvedAction))
            data.Log(new LogEntry($"{"Action",-14}: {resolvedAction}", Colors.White));
        if (!string.IsNullOrEmpty(resolvedProxy))
            data.Log(new LogEntry($"{"Proxy",-14}: {resolvedProxy}", Colors.White));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };

        var payload = new JObject
        {
            ["mode"]    = "turnstile",
            ["domain"]  = resolvedDomain,
            ["siteKey"] = resolvedSiteKey
        };
        if (!string.IsNullOrEmpty(resolvedAction))
            payload["action"] = resolvedAction;
        if (!string.IsNullOrEmpty(resolvedProxy))
            payload["proxy"] = resolvedProxy;

        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = client.PostAsync($"http://localhost:{port}/cloudflare", content).Result;
        }
        catch (Exception ex)
        {
            throw new Exception($"[Turnstile] Cannot reach cf-bypass on port {port}: {ex.Message}");
        }

        string body = response.Content.ReadAsStringAsync().Result;

        JObject json;
        try { json = JObject.Parse(body); }
        catch { throw new Exception($"[Turnstile] Bad response: {body}"); }

        if (!response.IsSuccessStatusCode)
        {
            string msg = json["message"]?.ToString() ?? body;
            throw new Exception($"[Turnstile] Server error {(int)response.StatusCode}: {msg}");
        }

        string token = json["token"]?.ToString();
        if (string.IsNullOrEmpty(token))
            throw new Exception($"[Turnstile] No token returned: {body}");

        sw.Stop();

        // ── Response (CapMonster-style) ───────────────────────────────────────
        data.Log(new LogEntry($"{"Elapsed",-14}: {sw.ElapsedMilliseconds} ms", Colors.White));

        var responseJson = new JObject
        {
            ["solution"] = new JObject { ["cfTurnstileResponse"] = token },
            ["status"]   = "ready",
            ["errorId"]  = 0,
            ["errorCode"] = null,
            ["errorDescription"] = null
        };
        string responseJsonStr = responseJson.ToString(Newtonsoft.Json.Formatting.None);
        data.Log(new LogEntry(responseJsonStr, Colors.GreenYellow));
        data.ResponseSource = responseJsonStr;

        string varName = ReplaceValues(outputVariable, data);
        data.Variables.Set(new CVar(varName, token));
        data.Log(new LogEntry($"Saved to <{varName}>", Colors.GreenYellow));
    }

    private static void EnsureCfBypassRunning(BotData data, int port)
    {
        // Already listening?
        if (IsPortOpen(port)) return;

        lock (_procLock)
        {
            if (IsPortOpen(port)) return;

            // Find cf-bypass.exe next to the running exe
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string cfExe = Path.Combine(exeDir, "Solvers", "cf-bypass.exe");

            if (!File.Exists(cfExe))
                throw new FileNotFoundException(
                    $"[Turnstile] cf-bypass.exe not found at {cfExe}. " +
                    "Copy it to the SilverBullet folder.");

            data.Log(new LogEntry("[Turnstile] Starting cf-bypass.exe...", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(cfExe, port.ToString())
            {
                WorkingDirectory = exeDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            _cfProcess = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_cfProcess);

            // Wait up to 30 s for the server to be ready
            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;

                if (_cfProcess.HasExited)
                    throw new Exception("[Turnstile] cf-bypass.exe exited unexpectedly.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[Turnstile] cf-bypass.exe did not open port {port} in 30 s.");

            // Give Chrome a moment to create its window then minimize it
            Thread.Sleep(800);
            MinimizeChromeWindows();

            data.Log(new LogEntry("[Turnstile] cf-bypass ready.", Colors.GreenYellow));
        }
    }

    private static void MinimizeChromeWindows()
    {
        // Collect PIDs of Chrome processes spawned after cf-bypass started
        var chromePids = new System.Collections.Generic.HashSet<uint>();
        foreach (var p in Diag.Process.GetProcessesByName("chrome"))
            chromePids.Add((uint)p.Id);

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (chromePids.Contains(pid))
                ShowWindow(hWnd, SW_MINIMIZE);
            return true;
        }, IntPtr.Zero);
    }

    private static bool IsPortOpen(int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect("127.0.0.1", port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(200);
            if (success) client.EndConnect(result);
            return success;
        }
        catch { return false; }
    }
}
