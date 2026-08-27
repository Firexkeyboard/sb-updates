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

public class BlockDataDome : BlockBase
{
    private static Diag.Process _ddProcess;
    private static readonly object _procLock = new object();

    static BlockDataDome()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { _ddProcess?.Kill(); } catch { }
        };
    }

    private string url             = "";
    private string proxy           = "";
    private int    port            = 9505;
    private string outputCookie    = "DD_COOKIE";
    private string outputUserAgent = "DD_UA";

    public string Url             { get => url;             set { url = value;             OnPropertyChanged("Url"); } }
    public string Proxy           { get => proxy;           set { proxy = value;           OnPropertyChanged("Proxy"); } }
    public int    Port            { get => port;            set { port = value;            OnPropertyChanged("Port"); } }
    public string OutputCookie    { get => outputCookie;    set { outputCookie = value;    OnPropertyChanged("OutputCookie"); } }
    public string OutputUserAgent { get => outputUserAgent; set { outputUserAgent = value; OnPropertyChanged("OutputUserAgent"); } }

    public BlockDataDome() { Label = "DATADOME"; }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        Url = LineParser.ParseLiteral(ref input, "Url");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputCookie = LineParser.ParseLiteral(ref input, "OutputCookie");

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
        bw.Label(Label).Token("DATADOME").Literal(Url).Literal(OutputCookie);
        if (OutputUserAgent != "DD_UA")         bw.Token("UA").Literal(OutputUserAgent);
        if (!string.IsNullOrEmpty(Proxy))        bw.Token("PROXY").Literal(Proxy);
        if (Port != 9505)                        bw.Token("PORT").Literal(Port.ToString());
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl   = ReplaceValues(url, data);
        string resolvedProxy = ReplaceValues(proxy, data);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            throw new Exception($"[DD] Invalid URL '{resolvedUrl}' — must start with http:// or https://");

        EnsureServerRunning(data, port);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        data.Log(new LogEntry("DataDome Cookie Solver", Colors.MediumPurple));
        data.Log(new LogEntry($"{"URL",-14}: {resolvedUrl}", Colors.White));
        if (!string.IsNullOrEmpty(resolvedProxy))
            data.Log(new LogEntry($"{"Proxy",-14}: {resolvedProxy}", Colors.White));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        var payload = new JObject
        {
            ["url"]   = resolvedUrl,
            ["proxy"] = resolvedProxy
        };

        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try { response = client.PostAsync($"http://localhost:{port}/datadome", content).Result; }
        catch (Exception ex) { throw new Exception($"[DD] Cannot reach server on port {port}: {ex.Message}"); }

        string body = response.Content.ReadAsStringAsync().Result;

        JObject json;
        try { json = JObject.Parse(body); }
        catch { throw new Exception($"[DD] Bad response: {body}"); }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[DD] Server error {(int)response.StatusCode}: {json["error"] ?? body}");

        string cookieStr = json["cookie_string"]?.ToString();
        string returnUa  = json["user_agent"]?.ToString();

        if (string.IsNullOrEmpty(cookieStr))
            throw new Exception($"[DD] No cookie returned: {body}");

        sw.Stop();

        data.Log(new LogEntry($"{"Elapsed",-14}: {sw.ElapsedMilliseconds} ms", Colors.White));

        var responseJson = new JObject
        {
            ["solution"]         = new JObject { ["cookie_string"] = cookieStr, ["userAgent"] = returnUa },
            ["status"]           = "ready",
            ["errorId"]          = 0,
            ["errorCode"]        = null,
            ["errorDescription"] = null
        };
        string responseJsonStr = responseJson.ToString(Newtonsoft.Json.Formatting.None);
        data.Log(new LogEntry(responseJsonStr, Colors.GreenYellow));
        data.ResponseSource = responseJsonStr;

        string cookieVar = ReplaceValues(outputCookie, data);
        string uaVar     = ReplaceValues(outputUserAgent, data);

        data.Variables.Set(new CVar(cookieVar, cookieStr));
        if (!string.IsNullOrEmpty(returnUa))
            data.Variables.Set(new CVar(uaVar, returnUa));

        data.Log(new LogEntry($"Saved cookies to <{cookieVar}>, UA to <{uaVar}>", Colors.GreenYellow));
    }

    private static void EnsureServerRunning(BotData data, int port)
    {
        if (IsPortOpen(port)) return;

        lock (_procLock)
        {
            if (IsPortOpen(port)) return;

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe    = Path.Combine(exeDir, "Solvers", "datadome.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException($"[DD] datadome.exe not found at {exe}");

            data.Log(new LogEntry("[DD] Starting datadome.exe...", Colors.Orange));

            var psi = new Diag.ProcessStartInfo(exe, port.ToString())
            {
                WorkingDirectory       = exeDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false
            };
            _ddProcess = Diag.Process.Start(psi);
            ChildProcessGuard.Track(_ddProcess);

            int waited = 0;
            while (!IsPortOpen(port) && waited < 300)
            {
                Thread.Sleep(100);
                waited++;
                if (_ddProcess.HasExited)
                    throw new Exception("[DD] datadome.exe exited unexpectedly.");
            }

            if (!IsPortOpen(port))
                throw new Exception($"[DD] datadome.exe did not open port {port} in 30s.");

            data.Log(new LogEntry("[DD] Server ready.", Colors.GreenYellow));
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
