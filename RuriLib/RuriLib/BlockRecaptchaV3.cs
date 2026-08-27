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

public class BlockRecaptchaV3 : BlockBase
{
    private static Diag.Process _rc3Process;
    private static readonly object _procLock = new object();

    static BlockRecaptchaV3()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { _rc3Process?.Kill(); } catch { }
        };
    }

    private string url            = "";
    private string siteKey        = "";
    private string action         = "submit";
    private int    port           = 9512;
    private string outputVariable = "RC3_TOKEN";

    public string Url            { get => url;            set { url = value;            OnPropertyChanged("Url"); } }
    public string SiteKey        { get => siteKey;        set { siteKey = value;        OnPropertyChanged("SiteKey"); } }
    public string Action         { get => action;         set { action = value;         OnPropertyChanged("Action"); } }
    public int    Port           { get => port;           set { port = value;           OnPropertyChanged("Port"); } }
    public string OutputVariable { get => outputVariable; set { outputVariable = value; OnPropertyChanged("OutputVariable"); } }

    public BlockRecaptchaV3() { Label = "RECAPTCHA-V3"; }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        Url     = LineParser.ParseLiteral(ref input, "Url");
        SiteKey = LineParser.ParseLiteral(ref input, "SiteKey");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");

        while (input != "")
        {
            string tok = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
            if (tok == "ACTION") { LineParser.ParseToken(ref input, TokenType.Parameter, true); Action = LineParser.ParseLiteral(ref input, "Action"); }
            else if (tok == "PORT") { LineParser.ParseToken(ref input, TokenType.Parameter, true); Port = int.Parse(LineParser.ParseLiteral(ref input, "Port")); }
            else break;
        }
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("RECAPTCHA-V3").Literal(Url).Literal(SiteKey).Literal(OutputVariable);
        if (Action != "submit") bw.Token("ACTION").Literal(Action);
        if (Port != 9512)       bw.Token("PORT").Literal(Port.ToString());
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl     = ReplaceValues(url, data);
        string resolvedSiteKey = ReplaceValues(siteKey, data);
        string resolvedAction  = ReplaceValues(action, data);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            throw new Exception($"[RC3] Invalid URL '{resolvedUrl}' — must start with http:// or https://");

        if (string.IsNullOrWhiteSpace(resolvedSiteKey))
            throw new Exception("[RC3] SiteKey is empty.");

        EnsureServerRunning(data, port);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        data.Log(new LogEntry("Google reCAPTCHA v3 Solver", Colors.LightBlue));
        data.Log(new LogEntry($"{"URL",-14}: {resolvedUrl}",     Colors.White));
        data.Log(new LogEntry($"{"SiteKey",-14}: {resolvedSiteKey}", Colors.White));
        data.Log(new LogEntry($"{"Action",-14}: {resolvedAction}",   Colors.White));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        var payload = new JObject
        {
            ["websiteURL"] = resolvedUrl,
            ["websiteKey"] = resolvedSiteKey,
            ["action"]     = resolvedAction
        };

        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try { response = client.PostAsync($"http://localhost:{port}/recaptcha-v3", content).Result; }
        catch (Exception ex) { throw new Exception($"[RC3] Cannot reach server on port {port}: {ex.Message}"); }

        string body = response.Content.ReadAsStringAsync().Result;

        JObject json;
        try { json = JObject.Parse(body); }
        catch { throw new Exception($"[RC3] Bad response: {body}"); }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[RC3] Server error {(int)response.StatusCode}: {json["error"] ?? body}");

        string token = json["token"]?.ToString();
        if (string.IsNullOrEmpty(token))
            throw new Exception($"[RC3] No token returned: {body}");

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
            string exe    = Path.Combine(exeDir, "recaptcha-v3.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"[RC3] recaptcha-v3.exe not found at {exe}");

            data.Log(new LogEntry("[RC3] Starting recaptcha-v3.exe...", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(exe, port.ToString())
            {
                WorkingDirectory       = exeDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false
            };
            _rc3Process = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_rc3Process);

            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;
                if (_rc3Process.HasExited)
                    throw new Exception("[RC3] recaptcha-v3.exe exited unexpectedly.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[RC3] recaptcha-v3.exe did not open port {port} in 30s.");

            data.Log(new LogEntry("[RC3] Server ready.", Colors.GreenYellow));
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
