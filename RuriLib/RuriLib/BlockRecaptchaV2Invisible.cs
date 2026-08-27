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

public class BlockRecaptchaV2Invisible : BlockBase
{
    private static Diag.Process _rc2iProcess;
    private static readonly object _procLock = new object();

    static BlockRecaptchaV2Invisible()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (_rc2iProcess == null || _rc2iProcess.HasExited) return;
                // Kill the entire process tree so Chrome (spawned by the exe) also closes
                Diag.Process.Start(new Diag.ProcessStartInfo
                {
                    FileName        = "taskkill",
                    Arguments       = $"/F /T /PID {_rc2iProcess.Id}",
                    UseShellExecute = false,
                    CreateNoWindow  = true
                })?.WaitForExit(3000);
            }
            catch { try { _rc2iProcess?.Kill(); } catch { } }
        };
    }

    private string url            = "";
    private string action         = "submit";
    private string proxy          = "";
    private int    port           = 9513;
    private string outputVariable = "RC2I_TOKEN";

    public string Url            { get => url;            set { url = value;            OnPropertyChanged("Url"); } }
    public string Action         { get => action;         set { action = value;         OnPropertyChanged("Action"); } }
    public string Proxy          { get => proxy;          set { proxy = value;          OnPropertyChanged("Proxy"); } }
    public int    Port           { get => port;           set { port = value;           OnPropertyChanged("Port"); } }
    public string OutputVariable { get => outputVariable; set { outputVariable = value; OnPropertyChanged("OutputVariable"); } }

    public BlockRecaptchaV2Invisible() { Label = "RECAPTCHA-V2-INVISIBLE"; }

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
            if      (tok == "ACTION") { LineParser.ParseToken(ref input, TokenType.Parameter, true); Action = LineParser.ParseLiteral(ref input, "Action"); }
            else if (tok == "PROXY")  { LineParser.ParseToken(ref input, TokenType.Parameter, true); Proxy  = LineParser.ParseLiteral(ref input, "Proxy"); }
            else if (tok == "PORT")   { LineParser.ParseToken(ref input, TokenType.Parameter, true); Port   = int.Parse(LineParser.ParseLiteral(ref input, "Port")); }
            else break;
        }
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("RECAPTCHA-V2-INVISIBLE").Literal(Url).Literal(OutputVariable);
        if (Action != "submit")              bw.Token("ACTION").Literal(Action);
        if (!string.IsNullOrEmpty(Proxy))    bw.Token("PROXY").Literal(Proxy);
        if (Port != 9513)                    bw.Token("PORT").Literal(Port.ToString());
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl    = ReplaceValues(url, data);
        string resolvedAction = ReplaceValues(action, data);
        string resolvedProxy  = ReplaceValues(proxy, data);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            throw new Exception($"[RC2I] Invalid URL '{resolvedUrl}' — must start with http:// or https://");

        EnsureServerRunning(data, port);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        data.Log(new LogEntry("reCAPTCHA v2 Invisible Solver", Colors.LightGreen));
        data.Log(new LogEntry($"{"URL",-14}: {resolvedUrl}",    Colors.White));
        data.Log(new LogEntry($"{"Action",-14}: {resolvedAction}", Colors.White));
        if (!string.IsNullOrEmpty(resolvedProxy))
            data.Log(new LogEntry($"{"Proxy",-14}: {resolvedProxy}", Colors.White));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        var payload = new JObject
        {
            ["websiteURL"] = resolvedUrl,
            ["action"]     = resolvedAction,
        };
        if (!string.IsNullOrEmpty(resolvedProxy))
            payload["proxy"] = resolvedProxy;

        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try { response = client.PostAsync($"http://localhost:{port}/recaptcha-v2", content).Result; }
        catch (Exception ex) { throw new Exception($"[RC2I] Cannot reach server on port {port}: {ex.Message}"); }

        string body = response.Content.ReadAsStringAsync().Result;

        JObject json;
        try { json = JObject.Parse(body); }
        catch { throw new Exception($"[RC2I] Bad response: {body}"); }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[RC2I] Server error {(int)response.StatusCode}: {json["error"] ?? body}");

        string token = json["token"]?.ToString();
        if (string.IsNullOrEmpty(token))
            throw new Exception($"[RC2I] No token returned: {body}");

        sw.Stop();

        data.Log(new LogEntry($"{"Elapsed",-14}: {sw.ElapsedMilliseconds} ms", Colors.White));

        var responseJson = new JObject
        {
            ["solution"]         = new JObject { ["gRecaptchaResponse"] = token },
            ["status"]           = "ready",
            ["errorId"]          = 0,
            ["errorCode"]        = null,
            ["errorDescription"] = null
        };
        string responseJsonStr = responseJson.ToString(Newtonsoft.Json.Formatting.None);
        data.Log(new LogEntry(responseJsonStr, Colors.GreenYellow));
        data.ResponseSource = responseJsonStr;

        string varName = ReplaceValues(outputVariable, data);
        data.Variables.Set(new CVar(varName, token));
        data.Log(new LogEntry($"Saved to <{varName}>", Colors.GreenYellow));
    }

    private static void EnsureServerRunning(BotData data, int port)
    {
        if (IsPortOpen(port)) return;

        lock (_procLock)
        {
            if (IsPortOpen(port)) return;

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe    = Path.Combine(exeDir, "Solvers", "recaptcha-v2-invisible.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"[RC2I] recaptcha-v2-invisible.exe not found at {exe}");

            data.Log(new LogEntry("[RC2I] Starting recaptcha-v2-invisible.exe...", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(exe, port.ToString())
            {
                WorkingDirectory       = exeDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                // Redirect stdin so isatty() returns False → script skips interactive
                // workers/proxy prompts and auto-uses its non-interactive defaults.
                RedirectStandardInput  = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false
            };
            _rc2iProcess = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_rc2iProcess);
            // Close the stdin pipe immediately — the script never reads from it.
            try { _rc2iProcess.StandardInput.Close(); } catch { }

            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;
                if (_rc2iProcess.HasExited)
                    throw new Exception("[RC2I] recaptcha-v2-invisible.exe exited unexpectedly.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[RC2I] recaptcha-v2-invisible.exe did not open port {port} in 30s.");

            data.Log(new LogEntry("[RC2I] Server ready.", Colors.GreenYellow));
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
