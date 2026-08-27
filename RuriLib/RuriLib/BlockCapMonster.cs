using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockCapMonster : BlockBase
{
    // ── CORE ──────────────────────────────────────────────────────────────────
    // taskType stores the base name (e.g. "RecaptchaV2").
    // The actual API type is computed: base + "Task" or base + "TaskProxyless"
    private string apiKey         = "";
    private string taskType       = "RecaptchaV2";
    private bool   useProxy       = false;
    private string websiteURL     = "";
    private string websiteKey     = "";
    private string outputVariable = "CAPMONSTER_TOKEN";

    // ── OPTIONAL – COMMON ─────────────────────────────────────────────────────
    private string userAgent = "";
    private string cookies   = "";

    // ── OPTIONAL – V2 ─────────────────────────────────────────────────────────
    private bool   isInvisible         = false;
    private string recaptchaDataSValue = "";

    // ── OPTIONAL – V3 ─────────────────────────────────────────────────────────
    private bool   isEnterprise = false;
    private double minScore     = 0.3;
    private string pageAction   = "";

    // ── OPTIONAL – GEETEST ────────────────────────────────────────────────────
    private string gt        = "";
    private string challenge = "";

    // ── OPTIONAL – IMAGE TO TEXT ──────────────────────────────────────────────
    private string imageBody = "";

    // ── OPTIONAL – TURNSTILE / FUNCAPTCHA DATA ────────────────────────────────
    private string captchaData = "";

    // ── FUNCAPTCHA ────────────────────────────────────────────────────────────
    private string funcaptchaApiJSSubdomain = "";

    // ── DATADOME ──────────────────────────────────────────────────────────────
    private string captchaUrl       = "";
    private string datadomeCookie   = "";
    private string datadomeVersion  = "";

    // ── IMPERVA / INCAPSULA ───────────────────────────────────────────────────
    private string incapsulaScriptUrl  = "";
    private string incapsulaCookies    = "";
    private string reese84UrlEndpoint  = "";

    // ── POLL DELAY ────────────────────────────────────────────────────────────
    private int pollDelayMs = 3000;

    // ── PROXY ─────────────────────────────────────────────────────────────────
    private string proxyType     = "http";
    private string proxyAddress  = "";
    private int    proxyPort     = 0;
    private string proxyLogin    = "";
    private string proxyPassword = "";

    // ── PROPERTIES ────────────────────────────────────────────────────────────
    public string ApiKey         { get => apiKey;         set { apiKey         = value; OnPropertyChanged("ApiKey");         } }
    public string TaskType       { get => taskType;       set { taskType       = value; OnPropertyChanged("TaskType");       } }
    public bool   UseProxy       { get => useProxy;       set { useProxy       = value; OnPropertyChanged("UseProxy");       } }
    public string WebsiteURL     { get => websiteURL;     set { websiteURL     = value; OnPropertyChanged("WebsiteURL");     } }
    public string WebsiteKey     { get => websiteKey;     set { websiteKey     = value; OnPropertyChanged("WebsiteKey");     } }
    public string OutputVariable { get => outputVariable; set { outputVariable = value; OnPropertyChanged("OutputVariable"); } }
    public string UserAgent      { get => userAgent;      set { userAgent      = value; OnPropertyChanged("UserAgent");      } }
    public string Cookies        { get => cookies;        set { cookies        = value; OnPropertyChanged("Cookies");        } }
    public bool   IsInvisible    { get => isInvisible;    set { isInvisible    = value; OnPropertyChanged("IsInvisible");    } }
    public string RecaptchaDataSValue { get => recaptchaDataSValue; set { recaptchaDataSValue = value; OnPropertyChanged("RecaptchaDataSValue"); } }
    public bool   IsEnterprise   { get => isEnterprise;   set { isEnterprise   = value; OnPropertyChanged("IsEnterprise");   } }
    public double MinScore       { get => minScore;       set { minScore       = value; OnPropertyChanged("MinScore");       } }
    public string PageAction     { get => pageAction;     set { pageAction     = value; OnPropertyChanged("PageAction");     } }
    public string Gt             { get => gt;             set { gt             = value; OnPropertyChanged("Gt");             } }
    public string Challenge      { get => challenge;      set { challenge      = value; OnPropertyChanged("Challenge");      } }
    public string ImageBody      { get => imageBody;      set { imageBody      = value; OnPropertyChanged("ImageBody");      } }
    public string CaptchaData      { get => captchaData;      set { captchaData      = value; OnPropertyChanged("CaptchaData");      } }
    public string FuncaptchaApiJSSubdomain { get => funcaptchaApiJSSubdomain; set { funcaptchaApiJSSubdomain = value; OnPropertyChanged("FuncaptchaApiJSSubdomain"); } }
    public string IncapsulaScriptUrl  { get => incapsulaScriptUrl;  set { incapsulaScriptUrl  = value; OnPropertyChanged("IncapsulaScriptUrl");  } }
    public string IncapsulaCookies    { get => incapsulaCookies;    set { incapsulaCookies    = value; OnPropertyChanged("IncapsulaCookies");    } }
    public string Reese84UrlEndpoint  { get => reese84UrlEndpoint;  set { reese84UrlEndpoint  = value; OnPropertyChanged("Reese84UrlEndpoint");  } }
    public string CaptchaUrl       { get => captchaUrl;       set { captchaUrl       = value; OnPropertyChanged("CaptchaUrl");       } }
    public string DatadomeCookie   { get => datadomeCookie;   set { datadomeCookie   = value; OnPropertyChanged("DatadomeCookie");   } }
    public string DatadomeVersion  { get => datadomeVersion;  set { datadomeVersion  = value; OnPropertyChanged("DatadomeVersion");  } }
    public string ProxyType      { get => proxyType;      set { proxyType      = value; OnPropertyChanged("ProxyType");      } }
    public string ProxyAddress   { get => proxyAddress;   set { proxyAddress   = value; OnPropertyChanged("ProxyAddress");   } }
    public int    ProxyPort      { get => proxyPort;      set { proxyPort      = value; OnPropertyChanged("ProxyPort");      } }
    public string ProxyLogin     { get => proxyLogin;     set { proxyLogin     = value; OnPropertyChanged("ProxyLogin");     } }
    public string ProxyPassword  { get => proxyPassword;  set { proxyPassword  = value; OnPropertyChanged("ProxyPassword");  } }
    public int    PollDelayMs    { get => pollDelayMs;    set { pollDelayMs    = value; OnPropertyChanged("PollDelayMs");    } }

    public BlockCapMonster() { Label = "CAPMONSTER"; }

    // Returns the actual CapMonster API task type string
    private static string ApiTaskType(string baseType, bool proxy)
    {
        return baseType switch
        {
            "RecaptchaV2"          => proxy ? "RecaptchaV2Task"               : "RecaptchaV2TaskProxyless",
            "RecaptchaV2Enterprise"=> proxy ? "RecaptchaV2EnterpriseTask"     : "RecaptchaV2EnterpriseTaskProxyless",
            "RecaptchaV3"          => proxy ? "RecaptchaV3Task"               : "RecaptchaV3TaskProxyless",
            "Turnstile"            => proxy ? "TurnstileTask"                 : "TurnstileTaskProxyless",
            "GeeTest"              => proxy ? "GeeTestTask"                   : "GeeTestTaskProxyless",
            "ImageToText"          => "ImageToTextTask",
            "FriendlyCaptcha"      => "FriendlyCaptchaTaskProxyless",
            "Amazon"               => proxy ? "AmazonTask"                    : "AmazonTaskProxyless",
            "DataDome"             => "CustomTask",
            "Basilisk"             => "CustomTask",
            "FunCaptcha"           => "FunCaptchaTask",
            "Imperva"              => "CustomTask",
            _                      => baseType  // fallback — pass raw
        };
    }

    // ── FROM LOLISCRIPT ───────────────────────────────────────────────────────
    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        ApiKey         = LineParser.ParseLiteral(ref input, "ApiKey");
        TaskType       = LineParser.ParseLiteral(ref input, "TaskType");
        WebsiteURL     = LineParser.ParseLiteral(ref input, "WebsiteURL");
        WebsiteKey     = LineParser.ParseLiteral(ref input, "WebsiteKey");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");

        while (input != "")
        {
            string tok = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
            switch (tok)
            {
                case "USEPROXY":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    UseProxy = LineParser.ParseLiteral(ref input, "UseProxy").ToLowerInvariant() == "true";
                    break;
                case "USERAGENT":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    UserAgent = LineParser.ParseLiteral(ref input, "UserAgent");
                    break;
                case "COOKIES":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    Cookies = LineParser.ParseLiteral(ref input, "Cookies");
                    break;
                case "INVISIBLE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    IsInvisible = LineParser.ParseLiteral(ref input, "IsInvisible").ToLowerInvariant() == "true";
                    break;
                case "DATASVALUE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    RecaptchaDataSValue = LineParser.ParseLiteral(ref input, "DataSValue");
                    break;
                case "ENTERPRISE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    IsEnterprise = LineParser.ParseLiteral(ref input, "IsEnterprise").ToLowerInvariant() == "true";
                    break;
                case "MINSCORE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    MinScore = double.Parse(LineParser.ParseLiteral(ref input, "MinScore"), CultureInfo.InvariantCulture);
                    break;
                case "ACTION":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    PageAction = LineParser.ParseLiteral(ref input, "PageAction");
                    break;
                case "GT":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    Gt = LineParser.ParseLiteral(ref input, "Gt");
                    break;
                case "CHALLENGE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    Challenge = LineParser.ParseLiteral(ref input, "Challenge");
                    break;
                case "IMAGEBODY":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ImageBody = LineParser.ParseLiteral(ref input, "ImageBody");
                    break;
                case "DATA":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    CaptchaData = LineParser.ParseLiteral(ref input, "CaptchaData");
                    break;
                case "INCAPSCRIPT":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    IncapsulaScriptUrl = LineParser.ParseLiteral(ref input, "IncapsulaScriptUrl");
                    break;
                case "INCAPCOOKIES":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    IncapsulaCookies = LineParser.ParseLiteral(ref input, "IncapsulaCookies");
                    break;
                case "REESE84":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    Reese84UrlEndpoint = LineParser.ParseLiteral(ref input, "Reese84UrlEndpoint");
                    break;
                case "FUNCSUBDOMAIN":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    FuncaptchaApiJSSubdomain = LineParser.ParseLiteral(ref input, "FuncaptchaApiJSSubdomain");
                    break;
                case "CAPTCHAURL":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    CaptchaUrl = LineParser.ParseLiteral(ref input, "CaptchaUrl");
                    break;
                case "DDCOOKIE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    DatadomeCookie = LineParser.ParseLiteral(ref input, "DatadomeCookie");
                    break;
                case "DDVERSION":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    DatadomeVersion = LineParser.ParseLiteral(ref input, "DatadomeVersion");
                    break;
                case "PROXYTYPE":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ProxyType = LineParser.ParseLiteral(ref input, "ProxyType");
                    break;
                case "PROXYADDR":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ProxyAddress = LineParser.ParseLiteral(ref input, "ProxyAddress");
                    break;
                case "PROXYPORT":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ProxyPort = int.Parse(LineParser.ParseLiteral(ref input, "ProxyPort"));
                    break;
                case "PROXYLOGIN":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ProxyLogin = LineParser.ParseLiteral(ref input, "ProxyLogin");
                    break;
                case "PROXYPASS":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    ProxyPassword = LineParser.ParseLiteral(ref input, "ProxyPassword");
                    break;
                case "POLLDELAY":
                    LineParser.ParseToken(ref input, TokenType.Parameter, true);
                    PollDelayMs = int.Parse(LineParser.ParseLiteral(ref input, "PollDelayMs"));
                    break;
                default:
                    return this;
            }
        }
        return this;
    }

    // ── TO LOLISCRIPT ─────────────────────────────────────────────────────────
    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("CAPMONSTER")
          .Literal(ApiKey).Literal(TaskType).Literal(WebsiteURL).Literal(WebsiteKey).Literal(OutputVariable);

        if (UseProxy)                                   bw.Token("USEPROXY").Literal("true");
        if (!string.IsNullOrEmpty(UserAgent))           bw.Token("USERAGENT").Literal(UserAgent);
        if (!string.IsNullOrEmpty(Cookies))             bw.Token("COOKIES").Literal(Cookies);
        if (IsInvisible)                                bw.Token("INVISIBLE").Literal("true");
        if (!string.IsNullOrEmpty(RecaptchaDataSValue)) bw.Token("DATASVALUE").Literal(RecaptchaDataSValue);
        if (IsEnterprise)                               bw.Token("ENTERPRISE").Literal("true");
        if (Math.Abs(MinScore - 0.3) > 0.001)          bw.Token("MINSCORE").Literal(MinScore.ToString("0.0##", CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(PageAction))          bw.Token("ACTION").Literal(PageAction);
        if (!string.IsNullOrEmpty(Gt))                  bw.Token("GT").Literal(Gt);
        if (!string.IsNullOrEmpty(Challenge))           bw.Token("CHALLENGE").Literal(Challenge);
        if (!string.IsNullOrEmpty(ImageBody))           bw.Token("IMAGEBODY").Literal(ImageBody);
        if (!string.IsNullOrEmpty(CaptchaData))         bw.Token("DATA").Literal(CaptchaData);
        if (!string.IsNullOrEmpty(IncapsulaScriptUrl))       bw.Token("INCAPSCRIPT").Literal(IncapsulaScriptUrl);
        if (!string.IsNullOrEmpty(IncapsulaCookies))          bw.Token("INCAPCOOKIES").Literal(IncapsulaCookies);
        if (!string.IsNullOrEmpty(Reese84UrlEndpoint))        bw.Token("REESE84").Literal(Reese84UrlEndpoint);
        if (!string.IsNullOrEmpty(FuncaptchaApiJSSubdomain)) bw.Token("FUNCSUBDOMAIN").Literal(FuncaptchaApiJSSubdomain);
        if (!string.IsNullOrEmpty(CaptchaUrl))               bw.Token("CAPTCHAURL").Literal(CaptchaUrl);
        if (!string.IsNullOrEmpty(DatadomeCookie))      bw.Token("DDCOOKIE").Literal(DatadomeCookie);
        if (!string.IsNullOrEmpty(DatadomeVersion))     bw.Token("DDVERSION").Literal(DatadomeVersion);
        if (UseProxy && !string.IsNullOrEmpty(ProxyAddress))
        {
            bw.Token("PROXYTYPE").Literal(ProxyType);
            bw.Token("PROXYADDR").Literal(ProxyAddress);
            bw.Token("PROXYPORT").Literal(ProxyPort.ToString());
            if (!string.IsNullOrEmpty(ProxyLogin))    bw.Token("PROXYLOGIN").Literal(ProxyLogin);
            if (!string.IsNullOrEmpty(ProxyPassword)) bw.Token("PROXYPASS").Literal(ProxyPassword);
        }
        if (PollDelayMs != 3000) bw.Token("POLLDELAY").Literal(PollDelayMs.ToString());
        return bw.ToString();
    }

    // ── PROCESS ───────────────────────────────────────────────────────────────
    public override void Process(BotData data)
    {
        base.Process(data);

        string key     = ReplaceValues(apiKey,         data);
        string base_   = ReplaceValues(taskType,       data);
        string url     = ReplaceValues(websiteURL,     data);
        string sk      = ReplaceValues(websiteKey,     data);
        string outVar  = ReplaceValues(outputVariable, data);
        string ua      = ReplaceValues(userAgent,      data);
        string ck      = ReplaceValues(cookies,        data);
        string sval    = ReplaceValues(recaptchaDataSValue, data);
        string act     = ReplaceValues(pageAction,     data);
        string rGt     = ReplaceValues(gt,             data);
        string rCh     = ReplaceValues(challenge,      data);
        string imgBody = ReplaceValues(imageBody,      data);
        string capD      = ReplaceValues(captchaData,             data);
        string funcSub     = ReplaceValues(funcaptchaApiJSSubdomain, data);
        string incapScript = ReplaceValues(incapsulaScriptUrl,      data);
        string incapCook   = ReplaceValues(incapsulaCookies,        data);
        string reese84     = ReplaceValues(reese84UrlEndpoint,      data);
        string capUrl      = ReplaceValues(captchaUrl,              data);
        string ddCook  = ReplaceValues(datadomeCookie, data);
        string ddVer   = ReplaceValues(datadomeVersion,data);
        string pType   = ReplaceValues(proxyType,      data);
        string pAddr   = ReplaceValues(proxyAddress,   data);
        string pLogin  = ReplaceValues(proxyLogin,     data);
        string pPass   = ReplaceValues(proxyPassword,  data);

        bool sendProxy = useProxy && !string.IsNullOrEmpty(pAddr);
        string type    = ApiTaskType(base_, sendProxy);

        bool isGeeTest  = base_.Equals("GeeTest",     StringComparison.OrdinalIgnoreCase);
        bool isImgText  = base_.Equals("ImageToText", StringComparison.OrdinalIgnoreCase);
        bool isDataDome = base_.Equals("DataDome",    StringComparison.OrdinalIgnoreCase);
        bool isBasilisk    = base_.Equals("Basilisk",    StringComparison.OrdinalIgnoreCase);
        bool isFunCaptcha  = base_.Equals("FunCaptcha", StringComparison.OrdinalIgnoreCase);
        bool isImperva     = base_.Equals("Imperva",    StringComparison.OrdinalIgnoreCase);
        bool isTurnstile= base_.Equals("Turnstile",   StringComparison.OrdinalIgnoreCase);
        bool isV3       = base_.Equals("RecaptchaV3", StringComparison.OrdinalIgnoreCase);

        // ── Build task object ─────────────────────────────────────────────────
        var task = new JObject { ["type"] = type };

        if (isImgText)
        {
            task["body"] = imgBody;
        }
        else if (isGeeTest)
        {
            task["websiteURL"] = url;
            task["gt"]         = rGt;
            task["challenge"]  = rCh;
        }
        else if (isDataDome)
        {
            task["class"]      = "DataDome";
            task["websiteURL"] = url;
            if (!string.IsNullOrEmpty(ua)) task["userAgent"] = ua;
            var meta = new JObject { ["captchaUrl"] = capUrl, ["datadomeCookie"] = ddCook };
            if (!string.IsNullOrEmpty(ddVer)) meta["datadomeVersion"] = ddVer;
            task["metadata"] = meta;
            task["proxyType"]    = pType;
            task["proxyAddress"] = pAddr;
            task["proxyPort"]    = proxyPort;
            if (!string.IsNullOrEmpty(pLogin)) task["proxyLogin"]    = pLogin;
            if (!string.IsNullOrEmpty(pPass))  task["proxyPassword"] = pPass;
        }
        else if (isImperva)
        {
            task["class"]      = "Imperva";
            task["websiteURL"] = url;
            if (!string.IsNullOrEmpty(ua)) task["userAgent"] = ua;
            var meta = new JObject { ["incapsulaScriptUrl"] = incapScript, ["incapsulaCookies"] = incapCook };
            if (!string.IsNullOrEmpty(reese84)) meta["reese84UrlEndpoint"] = reese84;
            task["metadata"] = meta;
            task["proxyType"]    = pType;
            task["proxyAddress"] = pAddr;
            task["proxyPort"]    = proxyPort;
            if (!string.IsNullOrEmpty(pLogin)) task["proxyLogin"]    = pLogin;
            if (!string.IsNullOrEmpty(pPass))  task["proxyPassword"] = pPass;
        }
        else
        {
            task["websiteURL"] = url;
            if (isFunCaptcha)
                task["websitePublicKey"] = sk;
            else
                task["websiteKey"] = sk;
        }

        if (isBasilisk)   task["class"] = "Basilisk";
        if (isFunCaptcha && !string.IsNullOrEmpty(funcSub)) task["funcaptchaApiJSSubdomain"] = funcSub;

        if (!isDataDome && !isImperva)
        {
            if (!string.IsNullOrEmpty(ua))   task["userAgent"] = ua;
            if (!string.IsNullOrEmpty(ck))   task["cookies"]   = ck;
        }
        if (isInvisible)                 task["isInvisible"] = true;
        if (!string.IsNullOrEmpty(sval)) task["recaptchaDataSValue"] = sval;
        if (isEnterprise)                task["isEnterprise"] = true;
        if (isV3)
        {
            task["minScore"] = minScore;
            if (!string.IsNullOrEmpty(act)) task["pageAction"] = act;
        }
        if (isTurnstile && !string.IsNullOrEmpty(act)) task["pageAction"] = act;
        if (!string.IsNullOrEmpty(capD)) task["data"] = capD;
        if (!isDataDome && !isImperva && sendProxy)
        {
            task["proxyType"]    = pType;
            task["proxyAddress"] = pAddr;
            task["proxyPort"]    = proxyPort;
            if (!string.IsNullOrEmpty(pLogin)) task["proxyLogin"]    = pLogin;
            if (!string.IsNullOrEmpty(pPass))  task["proxyPassword"] = pPass;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };

        // ── Step 1: createTask ────────────────────────────────────────────────
        var createPayload = new JObject { ["clientKey"] = key, ["task"] = task };
        string createJson = createPayload.ToString(Newtonsoft.Json.Formatting.None);
        const string EP_CREATE = "https://api.capmonster.cloud/createTask";
        const string EP_RESULT = "https://api.capmonster.cloud/getTaskResult";

        data.Log(new LogEntry($"<--- Executing Block CapMonster ({type}) --->", Colors.Cyan));

        var res1 = http.PostAsync(EP_CREATE, new StringContent(createJson, Encoding.UTF8, "application/json")).Result;
        string raw1 = res1.Content.ReadAsStringAsync().Result;
        LogFull(data, EP_CREATE, createJson, res1, raw1);
        data.Flush();

        var j1 = JObject.Parse(raw1);
        int errId = j1["errorId"]?.Value<int>() ?? -1;
        if (errId != 0)
        {
            data.Log(new LogEntry($"[CapMonster] Error {errId}: {j1["errorDescription"]}", Colors.Tomato));
            data.Flush();
            return;
        }

        long taskId = j1["taskId"]!.Value<long>();

        // ── Step 2: poll getTaskResult ────────────────────────────────────────
        var getPayload = new JObject { ["clientKey"] = key, ["taskId"] = taskId };
        string getJson = getPayload.ToString(Newtonsoft.Json.Formatting.None);

        JObject solution = null;
        JObject lastJ2   = null;

        for (int attempt = 1; attempt <= 120; attempt++)
        {
            Thread.Sleep(Math.Max(500, pollDelayMs));
            var res2  = http.PostAsync(EP_RESULT, new StringContent(getJson, Encoding.UTF8, "application/json")).Result;
            string raw2 = res2.Content.ReadAsStringAsync().Result;
            lastJ2 = JObject.Parse(raw2);

            bool isReady = lastJ2["status"]?.ToString() == "ready";
            if (isReady)
            {
                // Don't dump the full response body (contains the token we'll log cleanly below)
                LogHeaders(data, EP_RESULT, getJson, res2);
                data.Log(new LogEntry("Status: ready", Colors.GreenYellow));
                data.Flush();
                solution = lastJ2["solution"] as JObject;
                break;
            }

            LogFull(data, EP_RESULT, getJson, res2, raw2);
            data.Flush();
        }

        if (solution == null)
        {
            data.Log(new LogEntry($"[CapMonster] Timed out after 120 attempts. Last: {lastJ2}", Colors.Tomato));
            return;
        }

        // ── Step 3: extract token ─────────────────────────────────────────────
        string token;

        if (isImgText)
        {
            token = solution["text"]?.ToString();
        }
        else if (isGeeTest)
        {
            string chSol  = solution["challenge"]?.ToString() ?? "";
            string valSol = solution["validate"]?.ToString()  ?? "";
            string secSol = solution["seccode"]?.ToString()   ?? "";
            token = chSol;
            data.Variables.Set(new CVar(outVar + "_VALIDATE", valSol));
            data.Variables.Set(new CVar(outVar + "_SECCODE",  secSol));
            data.Log(new LogEntry($"Saved to <{outVar}_VALIDATE>", Colors.GreenYellow));
            data.Log(new LogEntry($"Saved to <{outVar}_SECCODE>",  Colors.GreenYellow));
        }
        else if (isTurnstile || isBasilisk || isFunCaptcha)
        {
            token = solution["token"]?.ToString();
        }
        else if (isDataDome || isImperva)
        {
            token = solution["cookie"]?.ToString() ?? solution["cookies"]?.ToString() ?? solution.ToString();
        }
        else
        {
            token = solution["gRecaptchaResponse"]?.ToString();
        }

        if (string.IsNullOrEmpty(token))
        {
            data.Log(new LogEntry($"[CapMonster] No token in solution: {solution}", Colors.Tomato));
            return;
        }

        var finalJson = new JObject
        {
            ["errorId"]  = 0,
            ["status"]   = "ready",
            ["taskId"]   = taskId,
            ["solution"] = solution
        };
        data.ResponseSource = finalJson.ToString(Newtonsoft.Json.Formatting.None);

        data.Variables.Set(new CVar(outVar, token));
        data.Log(new LogEntry($"Saved to <{outVar}>", Colors.GreenYellow));
    }

    // ── LOG HELPER — colores idénticos al bloque REQUEST ─────────────────────
    private static void LogFull(BotData data, string url, string postData,
                                HttpResponseMessage response, string responseBody)
    {
        int code = (int)response.StatusCode;

        data.Log(new LogEntry($"Calling URL: {url}",         Colors.MediumTurquoise));
        data.Log(new LogEntry($"Post Data: {postData}",      Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Headers:",                Colors.DarkTurquoise));
        data.Log(new LogEntry("Content-Type: application/json", Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Cookies:",                Colors.MediumTurquoise));
        data.Log(new LogEntry("",                             Colors.Transparent));
        data.Log(new LogEntry($"Address: {url}",              Colors.Cyan));
        data.Log(new LogEntry($"Response code: {code} ({response.StatusCode})", Colors.Cyan));
        data.Log(new LogEntry("Received headers:",            Colors.DeepPink));

        foreach (var h in response.Headers)
            data.Log(new LogEntry($"{h.Key}: {string.Join(", ", h.Value)}", Colors.LightPink));
        foreach (var h in response.Content.Headers)
            data.Log(new LogEntry($"{h.Key}: {string.Join(", ", h.Value)}", Colors.LightPink));

        data.Log(new LogEntry("Received cookies:",            Colors.Goldenrod));
        data.Log(new LogEntry("Response Source:",             Colors.Green));
        data.Log(new LogEntry(responseBody,                   Colors.GreenYellow));
    }

    private static void LogHeaders(BotData data, string url, string postData,
                                   HttpResponseMessage response)
    {
        int code = (int)response.StatusCode;
        data.Log(new LogEntry($"Calling URL: {url}",         Colors.MediumTurquoise));
        data.Log(new LogEntry($"Post Data: {postData}",      Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Headers:",                Colors.DarkTurquoise));
        data.Log(new LogEntry("Content-Type: application/json", Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Cookies:",                Colors.MediumTurquoise));
        data.Log(new LogEntry("",                             Colors.Transparent));
        data.Log(new LogEntry($"Address: {url}",              Colors.Cyan));
        data.Log(new LogEntry($"Response code: {code} ({response.StatusCode})", Colors.Cyan));
        data.Log(new LogEntry("Received headers:",            Colors.DeepPink));
        foreach (var h in response.Headers)
            data.Log(new LogEntry($"{h.Key}: {string.Join(", ", h.Value)}", Colors.LightPink));
        foreach (var h in response.Content.Headers)
            data.Log(new LogEntry($"{h.Key}: {string.Join(", ", h.Value)}", Colors.LightPink));
        data.Log(new LogEntry("Received cookies:",            Colors.Goldenrod));
    }
}
