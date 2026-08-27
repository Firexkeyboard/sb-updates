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

public class BlockCfClearance : BlockBase
{
    private static Diag.Process _cfProcess;
    private static readonly object _procLock = new object();

    static BlockCfClearance()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { _cfProcess?.Kill(); } catch { }
        };
    }

    private string url            = "";
    private int    timeout        = 30;
    private int    port           = 9516;
    private string outputVariable = "CF_CLEARANCE";
    private bool   storeCookies   = false;

    public string Url            { get => url;            set { url = value;            OnPropertyChanged("Url"); } }
    public int    Timeout        { get => timeout;        set { timeout = value;        OnPropertyChanged("Timeout"); } }
    public int    Port           { get => port;           set { port = value;           OnPropertyChanged("Port"); } }
    public string OutputVariable { get => outputVariable; set { outputVariable = value; OnPropertyChanged("OutputVariable"); } }
    public bool   StoreCookies   { get => storeCookies;   set { storeCookies = value;   OnPropertyChanged("StoreCookies"); } }

    public BlockCfClearance() { Label = "CF-CLEARANCE"; }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        Url = LineParser.ParseLiteral(ref input, "Url");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");

        while (input != "")
        {
            string tok = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
            if      (tok == "TIMEOUT")      { LineParser.ParseToken(ref input, TokenType.Parameter, true); Timeout      = int.Parse(LineParser.ParseLiteral(ref input, "Timeout")); }
            else if (tok == "PORT")         { LineParser.ParseToken(ref input, TokenType.Parameter, true); Port         = int.Parse(LineParser.ParseLiteral(ref input, "Port")); }
            else if (tok == "STORECOOKIES") { LineParser.ParseToken(ref input, TokenType.Parameter, true); StoreCookies = bool.Parse(LineParser.ParseLiteral(ref input, "StoreCookies")); }
            else break;
        }
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("CF-CLEARANCE").Literal(Url).Literal(OutputVariable);
        if (Timeout != 30)       bw.Token("TIMEOUT").Literal(Timeout.ToString());
        if (Port    != 9516)     bw.Token("PORT").Literal(Port.ToString());
        if (StoreCookies)        bw.Token("STORECOOKIES").Literal("True");
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl = ReplaceValues(url, data);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            throw new Exception($"[CF] Invalid URL '{resolvedUrl}' — must start with http:// or https://");

        EnsureServerRunning(data, port);

        var sw = Diag.Stopwatch.StartNew();

        data.Log(new LogEntry("Cloudflare CF Clearance Solver", Colors.LightBlue));
        data.Log(new LogEntry($"{"URL",-14}: {resolvedUrl}", Colors.White));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout + 15) };

        var payload = new JObject
        {
            ["websiteURL"] = resolvedUrl,
            ["timeout"]    = timeout
        };

        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try { response = client.PostAsync($"http://localhost:{port}/cf-clearance", content).Result; }
        catch (Exception ex) { throw new Exception($"[CF] Cannot reach server on port {port}: {ex.Message}"); }

        string body = response.Content.ReadAsStringAsync().Result;

        JObject json;
        try { json = JObject.Parse(body); }
        catch { throw new Exception($"[CF] Bad response: {body}"); }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[CF] Server error {(int)response.StatusCode}: {json["error"] ?? body}");

        string token = json["cf_clearance"]?.ToString();
        if (string.IsNullOrEmpty(token))
            throw new Exception($"[CF] No cf_clearance returned: {body}");

        sw.Stop();
        data.Log(new LogEntry($"{"Elapsed",-14}: {sw.ElapsedMilliseconds} ms", Colors.White));

        string varName = ReplaceValues(outputVariable, data);

        string ua = json["user_agent"]?.ToString() ?? "";
        if (!string.IsNullOrEmpty(ua))
        {
            data.Log(new LogEntry($"{"User-Agent",-14}: {ua}", Colors.White));
            data.Variables.Set(new CVar(varName + "_UA", ua));
        }

        if (StoreCookies && json["cookies"] is JObject cookies)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in cookies)
            {
                data.Cookies[kv.Key] = kv.Value?.ToString() ?? "";
                parts.Add($"{kv.Key}={kv.Value}");
            }
            string cookieHeader = string.Join("; ", parts);
            data.Log(new LogEntry($"Cookie: {cookieHeader}", Colors.GreenYellow));
            data.Variables.Set(new CVar(varName, cookieHeader));
        }
        else
        {
            data.Log(new LogEntry($"Cookie: cf_clearance={token}", Colors.GreenYellow));
            data.Variables.Set(new CVar(varName, $"cf_clearance={token}"));
        }

        data.ResponseSource = body;
        data.Log(new LogEntry($"Saved to <{varName}>", Colors.GreenYellow));

        if (json["client_hints"] is JObject hints && hints.Count > 0)
        {
            foreach (var kv in hints)
            {
                string hVal = kv.Value?.ToString() ?? "";
                string hVar = varName + "_" + kv.Key.Replace("-", "_").ToUpper();
                data.Log(new LogEntry($"{kv.Key,-30}: {hVal}", Colors.Cyan));
                data.Variables.Set(new CVar(hVar, hVal));
            }
        }
    }

    private static void EnsureServerRunning(BotData data, int port)
    {
        if (IsPortOpen(port)) return;

        lock (_procLock)
        {
            if (IsPortOpen(port)) return;

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe    = Path.Combine(exeDir, "Solvers", "cf-clearance.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"[CF] cf-clearance.exe not found at {exe}");

            data.Log(new LogEntry("[CF] Starting cf-clearance.exe...", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(exe)
            {
                WorkingDirectory       = exeDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false
            };
            _cfProcess = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_cfProcess);

            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;
                if (_cfProcess.HasExited)
                    throw new Exception("[CF] cf-clearance.exe exited unexpectedly.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[CF] cf-clearance.exe did not open port {port} in 30s.");

            data.Log(new LogEntry("[CF] Server ready.", Colors.GreenYellow));
        }
    }

    private static bool IsPortOpen(int port)
    {
        try
        {
            using var c = new TcpClient();
            var r = c.BeginConnect("127.0.0.1", port, null, null);
            bool ok = r.AsyncWaitHandle.WaitOne(200);
            if (ok) c.EndConnect(r);
            return ok;
        }
        catch { return false; }
    }
}
