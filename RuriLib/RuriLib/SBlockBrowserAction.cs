using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using RuriLib.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using RuriLib.Functions.Files;
using RuriLib.Functions.UserAgent;
using RuriLib.LS;
using RuriLib.ViewModels;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace RuriLib;

public class SBlockBrowserAction : BlockBase
{
	private BrowserAction action;
	private string input = "";
	private string sUserAgent;

	public BrowserAction Action
	{
		get => action;
		set { action = value; OnPropertyChanged("Action"); }
	}

	public string Input
	{
		get => input;
		set { input = value; OnPropertyChanged("Input"); }
	}

	public string SUserAgent
	{
		get => sUserAgent;
		set { sUserAgent = value; OnPropertyChanged("SUserAgent"); }
	}

	public SBlockBrowserAction()
	{
		base.Label = "BROWSER ACTION";
	}

	public override BlockBase FromLS(string line)
	{
		string text = line.Trim();
		if (text.StartsWith("#"))
			base.Label = LineParser.ParseLabel(ref text);
		Action = (BrowserAction)LineParser.ParseEnum(ref text, "ACTION", typeof(BrowserAction));
		if (text != string.Empty && !text.StartsWith("USERAGENT \""))
			Input = LineParser.ParseLiteral(ref text, "INPUT");
		if (Action == BrowserAction.Open && text != string.Empty)
			SUserAgent = LineParser.ParseLiteral(ref text, "USERAGENT");
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("BROWSERACTION").Token(Action).Literal(Input, "Input");
		if (Action == BrowserAction.Open && !string.IsNullOrEmpty(SUserAgent))
			blockWriter.Indent().Token("USERAGENT").Literal(SUserAgent);
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		if (data.Driver == null && action != 0)
		{
			data.Log(new LogEntry("Open a browser first!", Colors.White));
			throw new Exception("Browser not open");
		}
		string text = BlockBase.ReplaceValues(input, data);
		Actions actions = null;
		switch (action)
		{
		case BrowserAction.Open:
			if (!string.IsNullOrEmpty(SUserAgent))
				data.ConfigSettings.CustomUserAgent = BlockBase.ReplaceValues(SUserAgent, data);
			OpenBrowser(data, text);
			try { BlockBase.UpdateSeleniumData(data); } catch { }
			break;
		case BrowserAction.Close:
			data.Driver.Close();
			data.BrowserOpen = false;
			break;
		case BrowserAction.Quit:
			data.Driver.Quit();
			data.BrowserOpen = false;
			break;
		case BrowserAction.ClearCookies:
			data.Driver.Manage().Cookies.DeleteAllCookies();
			break;
		case BrowserAction.SendKeys:
		{
			actions = new Actions(data.Driver);
			string[] array = text.Split(new string[1] { "||" }, StringSplitOptions.None);
			foreach (string s in array)
			{
				switch (s)
				{
				case "<TAB>":       actions.SendKeys(Keys.Tab);      continue;
				case "<ENTER>":     actions.SendKeys(Keys.Enter);    continue;
				case "<BACKSPACE>": actions.SendKeys(Keys.Backspace); continue;
				case "<ESC>":       actions.SendKeys(Keys.Escape);   continue;
				}
				FieldInfo fi = typeof(Keys).GetFields()
					.FirstOrDefault(f => ("<" + f.Name + ">").Equals(s, StringComparison.InvariantCultureIgnoreCase));
				actions.SendKeys(fi != null ? fi.GetValue(null).ToString() : s);
			}
			actions.Perform();
			Thread.Sleep(1000);
			if (text.Contains("<ENTER>") || text.Contains("<BACKSPACE>"))
				BlockBase.UpdateSeleniumData(data);
			break;
		}
		case BrowserAction.Screenshot:
			Files.SaveScreenshot(data.Driver.GetScreenshot(), data);
			break;
		case BrowserAction.OpenNewTab:
			((IJavaScriptExecutor)data.Driver).ExecuteScript("window.open();", Array.Empty<object>());
			data.Driver.SwitchTo().Window(data.Driver.WindowHandles.Last());
			break;
		case BrowserAction.SwitchToTab:
			data.Driver.SwitchTo().Window(data.Driver.WindowHandles[int.Parse(text)]);
			BlockBase.UpdateSeleniumData(data);
			break;
		case BrowserAction.CloseCurrentTab:
			((IJavaScriptExecutor)data.Driver).ExecuteScript("window.close();", Array.Empty<object>());
			break;
		case BrowserAction.Refresh:
			data.Driver.Navigate().Refresh();
			break;
		case BrowserAction.Back:
			data.Driver.Navigate().Back();
			break;
		case BrowserAction.Forward:
			data.Driver.Navigate().Forward();
			break;
		case BrowserAction.Maximize:
			data.Driver.Manage().Window.Maximize();
			break;
		case BrowserAction.Minimize:
			data.Driver.Manage().Window.Minimize();
			break;
		case BrowserAction.FullScreen:
			data.Driver.Manage().Window.FullScreen();
			break;
		case BrowserAction.SetWidth:
			data.Driver.Manage().Window.Size = new Size(int.Parse(text), data.Driver.Manage().Window.Size.Height);
			break;
		case BrowserAction.SetHeight:
			data.Driver.Manage().Window.Size = new Size(data.Driver.Manage().Window.Size.Width, int.Parse(text));
			break;
		case BrowserAction.DOMtoSOURCE:
			data.ResponseSource = data.Driver.FindElement(By.TagName("body")).GetAttribute("innerHTML");
			break;
		case BrowserAction.GetCookies:
			foreach (OpenQA.Selenium.Cookie c in data.Driver.Manage().Cookies.AllCookies)
				try { data.Cookies.Add(c.Name, c.Value); } catch { }
			break;
		case BrowserAction.SetCookies:
		{
			string domain = Regex.Match(BlockBase.ReplaceValues(input, data),
				@"^(?:https?:\/\/)?(?:[^@\/\n]+@)?([^:\/?\n]+)").Groups[1].Value;
			foreach (KeyValuePair<string, string> cookie in data.Cookies)
				try { data.Driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(cookie.Key, cookie.Value, domain, "/", DateTime.MaxValue)); } catch { }
			break;
		}
		case BrowserAction.SwitchToDefault:
			data.Driver.SwitchTo().DefaultContent();
			break;
		case BrowserAction.SwitchToAlert:
			data.Driver.SwitchTo().Alert();
			break;
		case BrowserAction.SwitchToParentFrame:
			data.Driver.SwitchTo().ParentFrame();
			break;
		}
		data.Log(new LogEntry($"Executed browser action {action} on input {text}", Colors.White));
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// Public entry point
	// ═══════════════════════════════════════════════════════════════════════════

	public static void OpenBrowser(BotData data, string url = "")
	{
		if (!data.BrowserOpen)
		{
			data.Log(new LogEntry("Opening browser...", Colors.White));
			bool ok = data.GlobalSettings.Selenium.Browser switch
			{
				BrowserType.Chrome  => OpenChrome(data, url),
				BrowserType.Firefox => OpenFirefox(data, url),
				_                   => false
			};
			if (!ok || data.Driver == null) return;
			data.Driver.Manage().Timeouts().PageLoad =
				TimeSpan.FromSeconds(data.GlobalSettings.Selenium.PageLoadTimeout);
			data.Log(new LogEntry("Opened!", Colors.White));
			data.BrowserOpen = true;
			return;
		}
		try { BlockBase.UpdateSeleniumData(data); } catch { }
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// Chrome
	// ═══════════════════════════════════════════════════════════════════════════

	private static bool OpenChrome(BotData data, string url)
	{
		try
		{
			// Auto-download the chromedriver version that matches the installed Chrome.
			try
			{
				new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
			}
			catch
			{
				data.Log(new LogEntry("[Selenium] WebDriverManager unavailable — using chromedriver from PATH.", Colors.White));
			}

			var service = ChromeDriverService.CreateDefaultService();
			service.SuppressInitialDiagnosticInformation = true;
			service.HideCommandPromptWindow              = true;
			service.EnableVerboseLogging                 = false;

			var opts = new ChromeOptions();

			// ── Silence Chrome's own output ──────────────────────────────────
			opts.AddArgument("--log-level=3");
			opts.AddArgument("--silent");

			// ── Anti-detection (always applied) ──────────────────────────────
			opts.AddExcludedArgument("enable-automation");
			opts.AddArgument("--disable-blink-features=AutomationControlled");
			opts.AddAdditionalChromeOption("useAutomationExtension", false);
			opts.AddArgument("--disable-infobars");
			opts.AddArgument("--no-first-run");
			opts.AddArgument("--no-default-browser-check");
			opts.AddArgument("--lang=en-US,en");
			opts.AddArgument("--window-size=1366,768");

			if (data.GlobalSettings.Selenium.DisableAutomation)
				opts.AddArgument("--disable-automation");

			if (data.GlobalSettings.Selenium.FastStart)
				opts.AddArgument("--fast-start");

			// ── Binary ───────────────────────────────────────────────────────
			if (!string.IsNullOrWhiteSpace(data.GlobalSettings.Selenium.ChromeBinaryLocation))
				opts.BinaryLocation = data.GlobalSettings.Selenium.ChromeBinaryLocation;

			// ── Headless ─────────────────────────────────────────────────────
			if (data.GlobalSettings.Selenium.Headless || data.ConfigSettings.ForceHeadless)
			{
				opts.AddArgument("--headless=new");
				opts.AddArgument("--window-size=1366,768");
			}
			else if (data.GlobalSettings.Selenium.ChromeExtensions.Count > 0)
			{
				opts.AddExtensions(
					data.GlobalSettings.Selenium.ChromeExtensions
						.Where(e => e.EndsWith(".crx"))
						.Select(e => Path.Combine(Directory.GetCurrentDirectory(), "ChromeExtensions", e)));
			}

			// ── Misc ─────────────────────────────────────────────────────────
			if (data.ConfigSettings.DisableNotifications)
				opts.AddArgument("--disable-notifications");
			if (data.ConfigSettings.DefaultProfileDirectory)
				opts.AddArgument("--profile-directory=Default");
			if (!string.IsNullOrEmpty(data.ConfigSettings.CustomCMDArgs))
				opts.AddArgument(data.ConfigSettings.CustomCMDArgs);

			// ── User agent ───────────────────────────────────────────────────
			string ua = null;
			if (data.ConfigSettings.RandomUA)
				ua = UserAgent.Random(data.random);
			else if (!string.IsNullOrEmpty(data.ConfigSettings.CustomUserAgent))
				ua = data.ConfigSettings.CustomUserAgent;
			if (ua != null)
				opts.AddArgument("--user-agent=" + ua);

			// ── Images ───────────────────────────────────────────────────────
			if (data.ConfigSettings.DisableImageLoading)
			{
				opts.AddArgument("--disable-images");
				opts.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
			}

			// ── Proxy ────────────────────────────────────────────────────────
			if (data.UseProxies)
			{
				var px = data.Proxy;
				bool hasUser = !string.IsNullOrEmpty(px.Username);
				int.TryParse(px.Port, out int pport);

				if (hasUser)
				{
					// Chrome ignores credentials in --proxy-server and MV2 extensions
					// are being phased out in Chrome 127+.  Instead, we run a tiny
					// local TCP bridge on 127.0.0.1 that injects authentication for us.
					// Chrome connects to the bridge with no credentials needed.
					int bridgePort = StartAuthBridge(px.Host, pport, px.Username, px.Password ?? "", px.Type);
					opts.AddArgument($"--proxy-server=http://127.0.0.1:{bridgePort}");
					data.Log(new LogEntry($"[Selenium] Proxy bridge on 127.0.0.1:{bridgePort} → {px.Host}:{pport}", Colors.White));
				}
				else
				{
					string scheme = px.Type switch
					{
						ProxyType.Socks4  => "socks4",
						ProxyType.Socks4a => "socks4a",
						ProxyType.Socks5  => "socks5",
						_                 => "http"
					};
					opts.AddArgument($"--proxy-server={scheme}://{px.Host}:{px.Port}");
				}
			}

			if (data.ConfigSettings.AcceptInsecureCertificates)
				opts.AcceptInsecureCertificates = true;

			// ── Launch ───────────────────────────────────────────────────────
			var driver = new ChromeDriver(service, opts);
			ApplyStealthCDP(driver, ua);

			data.Driver = driver;
			if (!string.IsNullOrWhiteSpace(url))
				data.Driver.Url = url;

			return true;
		}
		catch (Exception ex)
		{
			data.Log(new LogEntry(ex.ToString(), Colors.White));
			return false;
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// Firefox
	// ═══════════════════════════════════════════════════════════════════════════

	private static bool OpenFirefox(BotData data, string url)
	{
		try
		{
			var service = FirefoxDriverService.CreateDefaultService();
			service.SuppressInitialDiagnosticInformation = true;
			service.HideCommandPromptWindow              = true;

			var profile = new FirefoxProfile();
			var opts    = new FirefoxOptions();
			opts.AddArgument("--log-level=3");

			if (!string.IsNullOrWhiteSpace(data.GlobalSettings.Selenium.FirefoxBinaryLocation))
				opts.BinaryLocation = data.GlobalSettings.Selenium.FirefoxBinaryLocation;
			if (data.GlobalSettings.Selenium.Headless || data.ConfigSettings.ForceHeadless)
				opts.AddArgument("--headless");
			if (data.ConfigSettings.DisableNotifications)
				profile.SetPreference("dom.webnotifications.enabled", false);
			if (data.ConfigSettings.DefaultProfileDirectory)
				opts.AddArgument("--profile-directory=Default");
			if (!string.IsNullOrEmpty(data.ConfigSettings.CustomCMDArgs))
				opts.AddArgument(data.ConfigSettings.CustomCMDArgs);
			if (data.GlobalSettings.Selenium.FastStart)
				opts.AddArgument("--fast-start");
			if (data.ConfigSettings.RandomUA)
				profile.SetPreference("general.useragent.override", UserAgent.Random(data.random));
			else if (!string.IsNullOrEmpty(data.ConfigSettings.CustomUserAgent))
				profile.SetPreference("general.useragent.override", data.ConfigSettings.CustomUserAgent);
			if (data.GlobalSettings.Selenium.DisableAutomation)
			{
				opts.AddArgument("--disable-automation");
				opts.AddAdditionalFirefoxOption("useAutomationExtension", false);
			}
			if (data.ConfigSettings.DisableImageLoading)
				profile.SetPreference("permissions.default.image", 2);

			if (data.UseProxies)
			{
				var px = data.Proxy;
				profile.SetPreference("network.proxy.type", 1);
				int.TryParse(px.Port, out int pport);

				if (px.Type == ProxyType.Http)
				{
					profile.SetPreference("network.proxy.http",      px.Host);
					profile.SetPreference("network.proxy.http_port", pport);
					profile.SetPreference("network.proxy.ssl",       px.Host);
					profile.SetPreference("network.proxy.ssl_port",  pport);
				}
				else
				{
					profile.SetPreference("network.proxy.socks",         px.Host);
					profile.SetPreference("network.proxy.socks_port",    pport);
					profile.SetPreference("network.proxy.socks_version",
						(px.Type == ProxyType.Socks4 || px.Type == ProxyType.Socks4a) ? 4 : 5);
					if (!string.IsNullOrEmpty(px.Username))
					{
						profile.SetPreference("network.proxy.socks_username", px.Username);
						profile.SetPreference("network.proxy.socks_password", px.Password ?? "");
					}
				}
			}

			if (data.ConfigSettings.AcceptInsecureCertificates)
				opts.AcceptInsecureCertificates = true;

			opts.Profile = profile;
			data.Driver  = (RemoteWebDriver)(object)new FirefoxDriver(service, opts, TimeSpan.FromMinutes(1));

			if (!string.IsNullOrWhiteSpace(url))
				data.Driver.Url = url;

			return true;
		}
		catch (Exception ex)
		{
			data.Log(new LogEntry(ex.ToString(), Colors.White));
			return false;
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// CDP stealth injection — patches JS fingerprints before every page load
	// ═══════════════════════════════════════════════════════════════════════════

	private static void ApplyStealthCDP(ChromeDriver driver, string userAgent = null)
	{
		const string js = @"(function(){
  // 1. navigator.webdriver → undefined
  Object.defineProperty(navigator,'webdriver',{get:()=>undefined,configurable:true});

  // 2. chrome runtime object (absent in automation)
  if(!window.chrome) window.chrome={};
  window.chrome.app={isInstalled:false,getDetails:function(){return null;},getIsInstalled:function(){return false;},runningState:function(){return'cannot_run';}};
  if(!window.chrome.runtime) window.chrome.runtime={};
  window.chrome.csi=function(){return{startE:Date.now(),onloadT:Date.now(),pageT:1,tran:15};};
  window.chrome.loadTimes=function(){return null;};

  // 3. Permissions API
  try{
    const orig=window.navigator.permissions.query.bind(navigator.permissions);
    window.navigator.permissions.query=p=>p.name==='notifications'?Promise.resolve({state:Notification.permission}):orig(p);
  }catch(e){}

  // 4. Realistic plugins
  Object.defineProperty(navigator,'plugins',{configurable:true,get:()=>{
    function fp(n,f,d){const p=Object.create(Plugin.prototype);Object.defineProperty(p,'name',{value:n});Object.defineProperty(p,'filename',{value:f});Object.defineProperty(p,'description',{value:d});Object.defineProperty(p,'length',{value:1});return p;}
    const arr=[fp('Chrome PDF Plugin','internal-pdf-viewer','Portable Document Format'),fp('Chrome PDF Viewer','mhjfbmdgcfjbbpaeojofohoefgiehjai',''),fp('Native Client','internal-nacl-plugin','')];
    Object.setPrototypeOf(arr,PluginArray.prototype);
    arr.item=i=>arr[i];arr.namedItem=n=>arr.find(p=>p.name===n)||null;arr.refresh=()=>{};return arr;
  }});

  // 5. Languages & hardware
  Object.defineProperty(navigator,'languages',{get:()=>['en-US','en'],configurable:true});
  Object.defineProperty(navigator,'hardwareConcurrency',{get:()=>4,configurable:true});

  // 6. WebGL fingerprint
  function pgwl(c){if(!c)return;const o=c.prototype.getParameter;c.prototype.getParameter=function(p){if(p===37445)return'Intel Inc.';if(p===37446)return'Intel Iris OpenGL Engine';return o.apply(this,arguments);};}
  pgwl(window.WebGLRenderingContext);try{pgwl(window.WebGL2RenderingContext);}catch(e){}

  // 7. Screen size consistent with --window-size
  try{Object.defineProperty(screen,'width',{get:()=>1366,configurable:true});Object.defineProperty(screen,'height',{get:()=>768,configurable:true});Object.defineProperty(screen,'availWidth',{get:()=>1366,configurable:true});Object.defineProperty(screen,'availHeight',{get:()=>728,configurable:true});}catch(e){}
})();";

		try
		{
			driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument",
				new Dictionary<string, object> { { "source", js } });

			if (!string.IsNullOrEmpty(userAgent))
			{
				driver.ExecuteCdpCommand("Network.setUserAgentOverride",
					new Dictionary<string, object>
					{
						{ "userAgent",      userAgent        },
						{ "acceptLanguage", "en-US,en;q=0.9" },
						{ "platform",       "Win32"          }
					});
			}
		}
		catch { }
	}

	// ═══════════════════════════════════════════════════════════════════════════
	// Local proxy authentication bridge
	// ═══════════════════════════════════════════════════════════════════════════
	//
	// Chrome cannot pass credentials via --proxy-server, and Manifest V2
	// extensions (webRequestBlocking) are deprecated in Chrome 127+.
	//
	// Solution: bind a TcpListener on 127.0.0.1:0 (OS picks a free port).
	// Chrome speaks plain HTTP to the bridge; the bridge authenticates with
	// the real upstream proxy on its behalf — no dialog ever appears.
	//
	// Supports:
	//   • HTTP/HTTPS upstream  → injects Proxy-Authorization header in CONNECT
	//   • SOCKS5 upstream      → performs SOCKS5 user/pass handshake, then
	//                            bridges Chrome's CONNECT to the SOCKS5 CONNECT
	// ═══════════════════════════════════════════════════════════════════════════

	private static int StartAuthBridge(string host, int port, string user, string pass, ProxyType type)
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int localPort = ((IPEndPoint)listener.LocalEndpoint).Port;

		_ = Task.Run(async () =>
		{
			while (true)
			{
				TcpClient client;
				try { client = await listener.AcceptTcpClientAsync(); }
				catch { break; }
				_ = Task.Run(() => BridgeClient(client, host, port, user, pass, type));
			}
		});

		return localPort;
	}

	private static async Task BridgeClient(TcpClient client,
		string upHost, int upPort, string user, string pass, ProxyType type)
	{
		using var _c = client;
		client.NoDelay = true;
		var cStream = client.GetStream();

		// Read the HTTP request from Chrome (always HTTP CONNECT for HTTPS targets)
		string req = await ReadHttpHeaders(cStream);
		if (string.IsNullOrEmpty(req)) return;

		using var upstream = new TcpClient();
		upstream.NoDelay = true;
		try { await upstream.ConnectAsync(upHost, upPort); }
		catch { return; }
		var uStream = upstream.GetStream();

		switch (type)
		{
			case ProxyType.Socks5:
				await BridgeViaSocks5(cStream, uStream, req, user, pass);
				break;
			case ProxyType.Socks4:
			case ProxyType.Socks4a:
				await BridgeViaSocks4(cStream, uStream, req, type == ProxyType.Socks4a);
				break;
			default:
				await BridgeViaHttp(cStream, uStream, req, user, pass);
				break;
		}
	}

	// ── HTTP / HTTPS upstream ─────────────────────────────────────────────────

	private static async Task BridgeViaHttp(Stream cStream, NetworkStream uStream,
		string req, string user, string pass)
	{
		// Inject Proxy-Authorization into the request Chrome already sent
		string authB64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
		string modReq  = req.TrimEnd('\r', '\n') + $"\r\nProxy-Authorization: Basic {authB64}\r\n\r\n";
		await uStream.WriteAsync(Encoding.ASCII.GetBytes(modReq));

		string resp = await ReadHttpHeaders(uStream);
		if (string.IsNullOrEmpty(resp)) return;
		await cStream.WriteAsync(Encoding.ASCII.GetBytes(resp));

		// For a successful CONNECT tunnel, pipe raw bytes both ways
		if (req.StartsWith("CONNECT ", StringComparison.OrdinalIgnoreCase) && resp.Contains(" 200 "))
			await Task.WhenAny(PipeAsync(cStream, uStream), PipeAsync(uStream, cStream));
		else if (!req.StartsWith("CONNECT ", StringComparison.OrdinalIgnoreCase))
			await PipeAsync(uStream, cStream);
	}

	// ── SOCKS5 upstream ───────────────────────────────────────────────────────

	private static async Task BridgeViaSocks5(Stream cStream, NetworkStream uStream,
		string req, string user, string pass)
	{
		string target = ParseConnectTarget(req);
		if (string.IsNullOrEmpty(target)) return;
		int c = target.LastIndexOf(':');
		string tHost = target.Substring(0, c);
		int.TryParse(target.Substring(c + 1), out int tPort);
		if (tPort == 0) tPort = 443;

		bool hasAuth = !string.IsNullOrEmpty(user);
		byte method  = hasAuth ? (byte)0x02 : (byte)0x00; // 0x02 = user/pass, 0x00 = no auth

		// Greeting
		await uStream.WriteAsync(new byte[] { 0x05, 0x01, method });
		byte[] authResp = new byte[2];
		if (await ReadExact(uStream, authResp) < 2 || authResp[0] != 0x05) return;

		if (authResp[1] == 0x02 && hasAuth)
		{
			// Username/password sub-negotiation (RFC 1929)
			byte[] userB = Encoding.UTF8.GetBytes(user);
			byte[] passB = Encoding.UTF8.GetBytes(pass ?? "");
			var msg = new byte[3 + userB.Length + passB.Length];
			msg[0] = 0x01;
			msg[1] = (byte)userB.Length;
			userB.CopyTo(msg, 2);
			msg[2 + userB.Length] = (byte)passB.Length;
			passB.CopyTo(msg, 3 + userB.Length);
			await uStream.WriteAsync(msg);

			byte[] sub = new byte[2];
			if (await ReadExact(uStream, sub) < 2 || sub[1] != 0x00) return; // 0x00 = OK
		}
		else if (authResp[1] != 0x00) return; // Server rejected our method

		// CONNECT command — domain name type (0x03)
		byte[] hostB  = Encoding.ASCII.GetBytes(tHost);
		var connectCmd = new byte[7 + hostB.Length];
		connectCmd[0] = 0x05; // version
		connectCmd[1] = 0x01; // CONNECT
		connectCmd[2] = 0x00; // reserved
		connectCmd[3] = 0x03; // domain name
		connectCmd[4] = (byte)hostB.Length;
		hostB.CopyTo(connectCmd, 5);
		connectCmd[5 + hostB.Length] = (byte)(tPort >> 8);
		connectCmd[6 + hostB.Length] = (byte)(tPort & 0xFF);
		await uStream.WriteAsync(connectCmd);

		// Read SOCKS5 reply
		byte[] reply = new byte[4];
		if (await ReadExact(uStream, reply) < 4 || reply[1] != 0x00) return; // 0x00 = succeeded
		// Skip bind address
		int skip = reply[3] switch { 0x01 => 4, 0x04 => 16, _ => uStream.ReadByte() };
		await ReadExact(uStream, new byte[skip + 2]); // address + 2 port bytes

		// Tell Chrome the CONNECT was successful
		await cStream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"));
		await Task.WhenAny(PipeAsync(cStream, uStream), PipeAsync(uStream, cStream));
	}

	// ── SOCKS4/4a upstream ────────────────────────────────────────────────────

	private static async Task BridgeViaSocks4(Stream cStream, NetworkStream uStream,
		string req, bool socks4a)
	{
		string target = ParseConnectTarget(req);
		if (string.IsNullOrEmpty(target)) return;
		int c = target.LastIndexOf(':');
		string tHost = target.Substring(0, c);
		int.TryParse(target.Substring(c + 1), out int tPort);
		if (tPort == 0) tPort = 443;

		byte[] ip;
		byte[] hostBytes = null;
		if (socks4a)
		{
			ip = new byte[] { 0x00, 0x00, 0x00, 0x01 }; // 0.0.0.1 signals SOCKS4a
			hostBytes = Encoding.ASCII.GetBytes(tHost + "\0");
		}
		else
		{
			try { ip = (await Dns.GetHostAddressesAsync(tHost))[0].GetAddressBytes(); }
			catch { return; }
		}

		int extraLen = hostBytes?.Length ?? 0;
		var req4 = new byte[9 + extraLen];
		req4[0] = 0x04;
		req4[1] = 0x01; // CONNECT
		req4[2] = (byte)(tPort >> 8);
		req4[3] = (byte)(tPort & 0xFF);
		ip.CopyTo(req4, 4);
		req4[8] = 0x00; // null user ID
		hostBytes?.CopyTo(req4, 9);

		await uStream.WriteAsync(req4);

		byte[] resp = new byte[8];
		if (await ReadExact(uStream, resp) < 8 || resp[1] != 0x5A) return; // 0x5A = granted

		await cStream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"));
		await Task.WhenAny(PipeAsync(cStream, uStream), PipeAsync(uStream, cStream));
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private static async Task<string> ReadHttpHeaders(Stream stream)
	{
		var sb = new StringBuilder(512);
		int b0 = 0, b1 = 0, b2 = 0, b3 = 0;
		byte[] buf = new byte[1];
		while (sb.Length < 65536)
		{
			int n;
			try { n = await stream.ReadAsync(buf, 0, 1); } catch { break; }
			if (n <= 0) break;
			b0 = b1; b1 = b2; b2 = b3; b3 = buf[0];
			sb.Append((char)buf[0]);
			if (b0 == '\r' && b1 == '\n' && b2 == '\r' && b3 == '\n') break;
		}
		return sb.ToString();
	}

	private static string ParseConnectTarget(string headers)
	{
		// "CONNECT host:port HTTP/1.1"
		string first = headers.Split('\n')[0].Trim();
		string[] parts = first.Split(' ');
		return parts.Length >= 2 ? parts[1] : null;
	}

	private static async Task<int> ReadExact(Stream stream, byte[] buf)
	{
		int total = 0;
		while (total < buf.Length)
		{
			int n;
			try { n = await stream.ReadAsync(buf, total, buf.Length - total); } catch { break; }
			if (n <= 0) break;
			total += n;
		}
		return total;
	}

	private static async Task PipeAsync(Stream src, Stream dst)
	{
		byte[] buf = new byte[65536];
		try
		{
			int n;
			while ((n = await src.ReadAsync(buf, 0, buf.Length)) > 0)
				await dst.WriteAsync(buf, 0, n);
		}
		catch { }
	}
}
