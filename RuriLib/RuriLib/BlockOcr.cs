using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using ExProxyType = RuriLib.Models.ProxyType;
using ImageProcessor;
using ImageProcessor.Imaging;
using OpenBullet.ImageProcessor;
using OpenBullet.ImageProcessor.Layers;
using RuriLib.Functions.EvalString;
using RuriLib.Functions.Requests;
using RuriLib.LS;
using RuriLib.Models;
using Svg;
using Tesseract;

namespace RuriLib;

public class BlockOcr : BlockBase
{
	private string variableName = "";

	private bool isCapture;

	private string url = "";

	private int width;

	private int height;

	private string userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";

	private string pixelFormat = "Default";

	private string engine = "Default";

	private string pageSeg = "SingleLine";

	private string ocrString = "";

	private bool isBase64;

	private string ocrLang = "eng";

	private ObservableCollection<string> languages = new ObservableCollection<string>();

	private SecurityProtocol securityProtocol;

	public string VariableName
	{
		get
		{
			return variableName;
		}
		set
		{
			variableName = value;
			OnPropertyChanged("VariableName");
		}
	}

	public bool IsCapture
	{
		get
		{
			return isCapture;
		}
		set
		{
			isCapture = value;
			OnPropertyChanged("IsCapture");
		}
	}

	public string Url
	{
		get
		{
			return url;
		}
		set
		{
			url = value.Replace("\n", "");
			OnPropertyChanged("Url");
		}
	}

	public int Width
	{
		get
		{
			return width;
		}
		set
		{
			width = value;
			OnPropertyChanged("Width");
		}
	}

	public int Height
	{
		get
		{
			return height;
		}
		set
		{
			height = value;
			OnPropertyChanged("Height");
		}
	}

	public string UserAgent
	{
		get
		{
			return userAgent;
		}
		set
		{
			userAgent = value;
			OnPropertyChanged("UserAgent");
		}
	}

	public List<(string, string, Type)> Processors { get; set; } = new List<(string, string, Type)>
	{
		("EntropyCrop", "EntropyCrop", typeof(byte)),
		("Contrast", "Contrast", typeof(int)),
		("ContrastEx", "ContrastEx", typeof(sbyte)),
		("Constrain", "Constrain", typeof(Size)),
		("Morphology", "MorphologyEx", typeof(MorphologyLayer)),
		("Resize", "ResizeEx", typeof(Size)),
		("Crop", "Crop", typeof(CropLayer)),
		("Brightness", "Brightness", typeof(int)),
		("Grayscale", "Grayscale", null),
		("SepiaTone", "SepiaTone", null),
		("Invert", "Invert", null),
		("ReplaceColor", "ReplaceColor", typeof(System.Drawing.Color)),
		("BackgroundColor", "BackgroundColor", typeof(System.Drawing.Color)),
		("ReduceNoise", "ReduceNoise", null),
		("FastNlMeansDenoisingColored", "FastNlMeansDenoisingColored", typeof(FastNlMeansDenoisingColoredLayer)),
		("Threshold", "ThresholdEx", typeof(ThresholdLayer)),
		("AdaptiveThreshold", "AdaptiveThreshold", typeof(AdaptiveThresholdLayer)),
		("ColorThreshold", "Threshold", typeof(float)),
		("Transparency", "Transparency", null),
		("Expend", "Expend", null),
		("Soften", "Soften", null),
		("Atomization", "Atomization", null),
		("Embossment", "Embossment", null),
		("Blur", "Blur", typeof(Size)),
		("GaussianBlur", "GaussianBlur", typeof(int)),
		("Gamma", "Gamma", typeof(float)),
		("Smooth", "Smooth", typeof(int)),
		("Median", "Median", typeof(int)),
		("Mean", "Mean", typeof(int)),
		("Sharpen", "Sharpen", typeof(int)),
		("GaussianSharpen", "GaussianSharpen", typeof(int)),
		("Halftone", "Halftone", typeof(bool)),
		("Hue", "Hue", typeof(HueLayer)),
		("Pixelate", "Pixelate", typeof(Size)),
		("Resolution", "Resolution", typeof(ResolutionLayer)),
		("Rotate", "Rotate", typeof(int)),
		("AutoRotate", "AutoRotate", null),
		("Tint", "Tint", typeof(System.Drawing.Color)),
		("Vignette", "Vignette", typeof(System.Drawing.Color)),
		("Saturation", "Saturation", typeof(int)),
		("RemoveBackground", "RemoveBackground", null),
		("FaceWhiten", "FaceWhiten", null),
		("Zoom", "Zoom", typeof(ZoomLayer)),
		("Alpha", "Alpha", typeof(int)),
		("Alignment", "Alignment", typeof(int))
	};

	public List<ImageFilter> Filters { get; set; } = new List<ImageFilter>();

	public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>
	{
		{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko" },
		{ "Pragma", "no-cache" },
		{ "Accept", "*/*" }
	};

	public List<string> PixelFormats { get; set; } = new List<string>();

	public string PixelFormat
	{
		get
		{
			return pixelFormat;
		}
		set
		{
			pixelFormat = value;
			OnPropertyChanged("PixelFormat");
		}
	}

	public string Engine
	{
		get
		{
			return engine;
		}
		set
		{
			engine = value;
			OnPropertyChanged("Engine");
		}
	}

	public string PageSeg
	{
		get
		{
			return pageSeg;
		}
		set
		{
			pageSeg = value;
			OnPropertyChanged("PageSeg");
		}
	}

	public string OcrString
	{
		get
		{
			return ocrString;
		}
		set
		{
			ocrString = value;
			OnPropertyChanged("OcrString");
		}
	}

	public bool Base64
	{
		get
		{
			return isBase64;
		}
		set
		{
			isBase64 = value;
			OnPropertyChanged("Base64");
		}
	}

	public string OcrLang
	{
		get
		{
			return ocrLang;
		}
		set
		{
			ocrLang = value;
			OnPropertyChanged("OcrLang");
		}
	}

	public float OcrRate { get; set; }

	public ObservableCollection<string> Languages
	{
		get
		{
			return languages;
		}
		set
		{
			languages = value;
			OnPropertyChanged("Languages");
		}
	}

	public SecurityProtocol SecurityProtocol
	{
		get
		{
			return securityProtocol;
		}
		set
		{
			securityProtocol = value;
			OnPropertyChanged("SecurityProtocol");
		}
	}

	public Image OriginalImage { get; set; }

	public Image ProcessedImage { get; set; }

	public BlockOcr()
	{
		base.Label = "OCR";
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		BlockBase.InsertVariable(data, IsCapture, recursive: false, GetOcr(data), VariableName);
		data.Log(new LogEntry($"OCR Rate: {data.OcrRate}%\n", Colors.LightGreen));
	}

	public string[] GetOcr(BotData data, Pix cap = null)
	{
		string text = string.Empty;
		PageSegMode value = PageSeg.ToEnum(PageSegMode.SingleLine);
		TesseractEngine tesseractEngine = null;
		try
		{
			tesseractEngine = data.OcrEngine.GetOrCreateEngine(data, OcrLang, Engine.ToEnum(EngineMode.Default));
		}
		catch (ArgumentOutOfRangeException ex)
		{
			data.Log(new LogEntry(ex.Message, Colors.Red));
			data.Status = BotStatus.ERROR;
			return null;
		}
		Pix pix = null;
		Page page = null;
		try
		{
			Pix obj = cap ?? GetOcrImage(data);
			pix = obj;
			using (obj)
			{
				if (pix == null)
				{
					data.Status = BotStatus.ERROR;
					throw new NullReferenceException("image not found");
				}
				using (page = tesseractEngine.Process(pix, value))
				{
					try
					{
						text = page.GetText();
					}
					catch (AccessViolationException ex2)
					{
						data.Log(new LogEntry(ex2.Message, Colors.Red));
						data.Status = BotStatus.ERROR;
						return null;
					}
					catch (InvalidOperationException ex3)
					{
						data.Log(new LogEntry(ex3.Message, Colors.Red));
						data.Status = BotStatus.ERROR;
						return null;
					}
					if (data != null)
					{
						data.OcrRate = page.GetMeanConfidence();
						OcrRate = data.OcrRate;
					}
					else
					{
						OcrRate = page.GetMeanConfidence();
					}
				}
				try
				{
					if (data.GlobalSettings.Ocr.SaveImageToCaptchasFolder)
					{
						SaveBitmap(text, PixToBitmap(pix));
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex4)
		{
			data.Log(new LogEntry(ex4.Message, Colors.Red));
		}
		finally
		{
			if (cap != null && !cap.IsDisposed)
			{
				cap.Dispose();
			}
			if (page != null && !page.IsDisposed)
			{
				page.Dispose();
			}
			if (pix != null && !pix.IsDisposed)
			{
				pix.Dispose();
			}
		}
		if (text.Contains("\n"))
		{
			return text.Split('\n').ToArray();
		}
		return new string[1] { text };
	}

	public string[] GetOcr(Bitmap bitmap, EngineMode engineMode, PageSegMode pageSegMode, bool evaluateMath = false)
	{
		Pix pix = null;
		List<string> list = new List<string>();
		string text = ".\\tessdata";
		if (!Directory.Exists(text))
		{
			if (!Directory.Exists("..\\tessdata"))
			{
				throw new DirectoryNotFoundException("tessdata not found!");
			}
			text = "..\\tessdata";
		}
		if (!File.Exists(text + "\\" + OcrLang + ".traineddata"))
		{
			throw new FileNotFoundException("Language '" + OcrLang + "' not found!");
		}
		using (TesseractEngine tesseractEngine = new TesseractEngine(text, OcrLang, engineMode))
		{
			tesseractEngine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ+-*/=");
			tesseractEngine.SetVariable("tessedit_unrej_any_wd", value: true);
			if (bitmap == null)
			{
				bitmap = (Bitmap)OriginalImage;
			}
			bitmap = ApplyFilters(bitmap);
			while (true)
			{
				try
				{
					pix = BitmapToPix(bitmap);
				}
				catch (InvalidOperationException ex)
				{
					if (ex.Message.Contains("Format32bppPArgb") || ex.Message.Contains("Format32bppArgb"))
					{
						bitmap = bitmap.ConvertPixelFormat(System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						continue;
					}
					if (ex.Message.Contains("Format24bppRgb"))
					{
						bitmap = bitmap.ConvertPixelFormat(System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
						continue;
					}
				}
				break;
			}
			Graphics graphics = Graphics.FromImage(bitmap);
			using (Page page = tesseractEngine.Process(pix, pageSegMode))
			{
				list.Add(page.GetText());
				try
				{
					if (evaluateMath)
					{
						string text2 = list.FirstOrDefault();
						text2 = GetNumericAndMathOp(text2.Trim());
						if (text2.Contains("="))
						{
							string text3 = new CodeDomCalculator(text2.Split('=')[0]).Calculate().ToString();
							list.Add(text2 + text3);
						}
						else
						{
							string text4 = new CodeDomCalculator(text2).Calculate().ToString();
							list.Add(text2 + "=" + text4);
						}
					}
				}
				catch
				{
				}
				OcrRate = page.GetMeanConfidence();
				using ResultIterator resultIterator = page.GetIterator();
				do
				{
					if (resultIterator.IsAtBeginningOf(PageIteratorLevel.Word))
					{
						resultIterator.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds);
						graphics.SmoothingMode = SmoothingMode.AntiAlias;
						graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Brushes.Blue), bounds.X1, bounds.Y1, bounds.Width, bounds.Height);
					}
					if (resultIterator.IsAtBeginningOf(PageIteratorLevel.Symbol) && resultIterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var _))
					{
						resultIterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var bounds3);
						graphics.SmoothingMode = SmoothingMode.AntiAlias;
						graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Brushes.Red), bounds3.X1, bounds3.Y1, bounds3.Width, bounds3.Height);
					}
				}
				while (resultIterator.Next(PageIteratorLevel.Word, PageIteratorLevel.Symbol) || resultIterator.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word) || resultIterator.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine) || resultIterator.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
			}
			ProcessedImage = bitmap.Clone() as Bitmap;
			graphics?.Dispose();
			if (evaluateMath)
			{
				return new string[1] { list.LastOrDefault() };
			}
			try
			{
				pix.Dispose();
			}
			catch
			{
			}
		}
		return list.ToArray();
	}

	private string GetNumericAndMathOp(string input)
	{
		input = new string(input.Where((char c) => !char.IsLetter(c)).ToArray());
		Match match = new Regex("([-+]?[0-9]*\\.?[0-9]+[\\/\\+\\-\\*])+([-+]?[0-9]*\\.?[0-9]+)").Match(input);
		if (match.Success)
		{
			return match.Value;
		}
		return input;
	}

	public Bitmap CreateNonIndexedImage(Bitmap src)
	{
		Bitmap bitmap = new Bitmap(src.Width, src.Height);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.DrawImage(src, 0, 0);
		return bitmap;
	}

	public Pix GetOcrImage(BotData data)
	{
		Pix result = null;
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		string text = BlockBase.ReplaceValues(Url, data);
		if (Base64)
		{
			byte[] array = Convert.FromBase64String(FixBase64ForImage(text));
			using MemoryStream stream = new MemoryStream(array, 0, array.Length);
			bitmap = (Bitmap)(OriginalImage = (Bitmap)Image.FromStream(stream));
			bitmap2 = CreateNonIndexedImage(bitmap);
			bitmap2 = ApplyFilters(bitmap2, data);
			bitmap2 = ChangePixelFormat(bitmap2);
			result = BitmapToPix(bitmap2);
			bitmap2.Dispose();
		}
		else
		{
			Request request = new Request();
			request.Setup(data.GlobalSettings, SecurityProtocol, autoRedirect: true, data.ConfigSettings.MaxRedirects);
			data.Log(new LogEntry("Calling URL: " + text, Colors.MediumTurquoise));
			request.SetStandardContent(string.Empty, "application/x-www-form-urlencoded", HttpMethod.GET, encodeContent: false, GetLogBuffer(data));
			if (data.UseProxies)
			{
				request.SetProxy(data.Proxy);
			}
			data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
			Dictionary<string, string> headers = CustomHeaders.Select((KeyValuePair<string, string> h) => new KeyValuePair<string, string>(BlockBase.ReplaceValues(h.Key, data), BlockBase.ReplaceValues(h.Value, data))).ToDictionary((KeyValuePair<string, string> h) => h.Key, (KeyValuePair<string, string> h) => h.Value);
			request.SetHeaders(headers, acceptEncoding: true, GetLogBuffer(data));
			data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));
			request.SetCookies(data.Cookies, GetLogBuffer(data));
			data.LogNewLine();
			try
			{
				(data.Address, data.ResponseCode, data.ResponseHeaders, data.Cookies) = request.Perform(text, HttpMethod.GET, GetLogBuffer(data), resToMemoryStream: true);
			}
			catch (Exception ex)
			{
				if (data.ConfigSettings.IgnoreResponseErrors)
				{
					data.Log(new LogEntry(ex.Message, Colors.Tomato));
					data.ResponseSource = ex.Message;
					return null;
				}
				throw;
			}
			data.ResponseSource = request.SaveString(readResponseSource: true, data.ResponseHeaders, GetLogBuffer(data));
			try
			{
				data.ResponseStream = request.GetMemoryStream();
			}
			catch
			{
			}
			data.IsImage = true;
			try
			{
				bitmap = (Bitmap)Image.FromStream(request.GetMemoryStream());
			}
			catch (ArgumentException ex2)
			{
				if (ex2.Message == "Parameter is not valid")
				{
					try
					{
						string extension = Path.GetExtension(text);
						if (extension == ".svg" || extension.StartsWith(".svg"))
						{
							using MemoryStream stream2 = request.GetMemoryStream();
							bitmap = SvgDocument.Open<SvgDocument>((Stream)stream2).Draw();
						}
					}
					catch (Exception ex3)
					{
						data.Status = BotStatus.ERROR;
						throw new Exception("[Set Captcha] " + ex3.Message);
					}
				}
			}
			catch (Exception ex4)
			{
				data.Status = BotStatus.ERROR;
				throw new Exception("[Set Captcha] " + ex4.Message);
			}
			OriginalImage = bitmap;
			bitmap2 = CreateNonIndexedImage(bitmap);
			bitmap2 = ApplyFilters(bitmap2, data);
			bitmap2 = ChangePixelFormat(bitmap2);
			bool flag = false;
			do
			{
				try
				{
					result = BitmapToPix(bitmap2);
					try
					{
						bitmap2.Dispose();
					}
					catch
					{
					}
				}
				catch (InvalidOperationException ex5)
				{
					if (ex5.Message.Contains("Format32bppPArgb") || ex5.Message.Contains("Format32bppArgb"))
					{
						bitmap2 = bitmap2.ConvertPixelFormat(System.Drawing.Imaging.PixelFormat.Format24bppRgb);
					}
					else if (ex5.Message.Contains("Format24bppRgb"))
					{
						bitmap2 = bitmap2.ConvertPixelFormat(System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
					}
					flag = true;
					continue;
				}
				break;
			}
			while (flag);
		}
		return result;
	}

	private Bitmap ChangePixelFormat(Bitmap bitmap)
	{
		if (PixelFormat == "Default")
		{
			return bitmap;
		}
		return bitmap.ConvertPixelFormat(PixelFormat.ToEnum(System.Drawing.Imaging.PixelFormat.Format32bppArgb));
	}

	public Bitmap GetOcrImage(bool applyFilter = true, CProxy proxy = null)
	{
		Bitmap bitmap = null;
		string text = Url;
		Bitmap bitmap2;
		if (Base64)
		{
			byte[] array = Convert.FromBase64String(FixBase64ForImage(text));
			using MemoryStream stream = new MemoryStream(array, 0, array.Length);
			bitmap = (Bitmap)(OriginalImage = (Bitmap)Image.FromStream(stream));
			bitmap2 = CreateNonIndexedImage(bitmap);
			if (applyFilter)
			{
				bitmap2 = ApplyFilters(bitmap2);
			}
		}
		else
		{
			Request request = new Request();
			request.Setup(null, SecurityProtocol);
			if (proxy != null)
			{
				request.SetProxy(proxy);
			}
			try
			{
				request.Perform(text, HttpMethod.GET, null, resToMemoryStream: true);
			}
			catch (Exception)
			{
				throw;
			}
			try
			{
				bitmap = (Bitmap)Image.FromStream(request.GetMemoryStream());
			}
			catch (ArgumentException ex2)
			{
				if (ex2.Message == "Parameter is not valid.")
				{
					try
					{
						string extension = Path.GetExtension(text);
						if (extension == ".svg" || extension.StartsWith(".svg"))
						{
							using MemoryStream stream2 = request.GetMemoryStream();
							bitmap = SvgDocument.Open<SvgDocument>((Stream)stream2).Draw();
						}
					}
					catch (Exception)
					{
						throw;
					}
				}
			}
			catch (Exception)
			{
				throw;
			}
			OriginalImage = bitmap;
			bitmap2 = CreateNonIndexedImage(bitmap);
			if (applyFilter)
			{
				bitmap2 = ApplyFilters(bitmap2);
			}
		}
		return bitmap2;
	}

	private List<LogEntry> GetLogBuffer(BotData data)
	{
		if (!data.GlobalSettings.General.EnableBotLog && !data.IsDebug)
		{
			return null;
		}
		return data.LogBuffer;
	}

	public static string[] ParseString(string input, char separator, int count)
	{
		return (from s in input.Split(new char[1] { separator }, count)
			select s.Trim()).ToArray();
	}

	public string GetCustomHeaders()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> customHeader in CustomHeaders)
		{
			stringBuilder.Append(customHeader.Key + ": " + customHeader.Value);
			if (!customHeader.Equals(CustomHeaders.Last()))
			{
				stringBuilder.Append(Environment.NewLine);
			}
		}
		return stringBuilder.ToString();
	}

	public string GetFilters()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ImageFilter filter in Filters)
		{
			stringBuilder.Append(filter.Name + ": " + filter.Value);
			if (!filter.Equals(Filters.Last()))
			{
				stringBuilder.Append(Environment.NewLine);
			}
		}
		return stringBuilder.ToString();
	}

	public void SetCustomHeaders(string[] lines)
	{
		CustomHeaders.Clear();
		foreach (string text in lines)
		{
			if (text.Contains(':'))
			{
				string[] array = text.Split(new char[1] { ':' }, 2);
				CustomHeaders[array[0].Trim()] = array[1].Trim();
			}
		}
	}

	public void SetFilters(string[] lines)
	{
		Filters.Clear();
		foreach (string text in lines)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				string text2 = text;
				if (!text2.Contains(":"))
				{
					text2 += ":";
				}
				string[] array = text2.Split(new char[1] { ':' }, 2);
				Filters.Add(new ImageFilter
				{
					Name = array[0].Trim(),
					Value = array[1].Trim()
				});
			}
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter writer = new BlockWriter(GetType(), indent, base.Disabled);
		writer.Label(base.Label).Token("OCR").Token(OcrLang)
			.Literal(Url)
			.Boolean(Base64, "Base64");
		if (!writer.CheckDefault(VariableName, "VariableName"))
		{
			writer.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName)
				.Indent();
		}
		if (!string.IsNullOrWhiteSpace(Engine) && Engine != "Default")
		{
			writer.Indent().Token("Engine".ToUpper()).Token(Engine, "Engine");
		}
		if (!string.IsNullOrWhiteSpace(PageSeg) && PageSeg != "SingleLine")
		{
			writer.Indent().Token("PAGESEGMENT").Token(PageSeg, "PageSeg");
		}
		if (!string.IsNullOrWhiteSpace(PixelFormat) && PixelFormat != "Default")
		{
			writer.Indent().Token("PIXELFORMAT").Token(PixelFormat, "PixelFormat");
		}
		if (SecurityProtocol != 0)
		{
			writer.Indent().Token("SECPROTO").Token(SecurityProtocol, "SecurityProtocol");
		}
		writer.Indent();
		CustomHeaders.ToList().ForEach(delegate(KeyValuePair<string, string> header)
		{
			writer.Token("header".ToUpper()).Literal(header.Key + ": " + header.Value).Indent();
		});
		Filters.ToList().ForEach(delegate(ImageFilter filter)
		{
			writer.Token("filter".ToUpper()).Literal(filter.Name + ": " + filter.Value).Indent();
		});
		return writer.ToString();
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		OcrLang = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
		Url = LineParser.ParseLiteral(ref input, "Url");
		while (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		try
		{
			string text = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			if (text.ToUpper() == "VAR" || text.ToUpper() == "CAP")
			{
				IsCapture = text.ToUpper() == "CAP";
			}
		}
		catch
		{
			throw new ArgumentException("Invalid or missing variable type");
		}
		try
		{
			VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
		while (input != "" && LineParser.Lookahead(ref input) == TokenType.Parameter)
		{
			switch (LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper())
			{
			case "HEADER":
			{
				KeyValuePair<string, string> keyValuePair = ParsePair(LineParser.ParseLiteral(ref input, "HEADER VALUE"));
				CustomHeaders[keyValuePair.Key] = keyValuePair.Value;
				break;
			}
			case "FILTER":
			{
				KeyValuePair<string, string> keyValuePair2 = ParsePair(LineParser.ParseLiteral(ref input, "FILTER VALUE"), nullValue: true);
				Filters.Add(new ImageFilter
				{
					Name = keyValuePair2.Key,
					Value = keyValuePair2.Value
				});
				break;
			}
			case "PIXELFORMAT":
				PixelFormat = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
				break;
			case "ENGINE":
				Engine = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
				break;
			case "PAGESEGMENT":
				PageSeg = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
				break;
			case "SECPROTO":
				SecurityProtocol = LineParser.ParseEnum(ref input, "Security Protocol", typeof(SecurityProtocol));
				break;
			}
		}
		return this;
	}

	public static KeyValuePair<string, string> ParsePair(string pair, bool nullValue = false)
	{
		if (nullValue && !pair.Contains(":"))
		{
			pair += ":";
		}
		string[] array = pair.Split(new char[1] { ':' }, 2);
		return new KeyValuePair<string, string>(array[0].Trim(), array[1].Trim());
	}

	public string FixBase64ForImage(string Image)
	{
		StringBuilder stringBuilder = new StringBuilder(Image, Image.Length);
		stringBuilder.Replace("\r\n", string.Empty);
		stringBuilder.Replace(" ", string.Empty);
		stringBuilder.Replace("\\/", "/");
		if (stringBuilder.ToString().Contains("base64,"))
		{
			return stringBuilder.ToString().Split(',')[1];
		}
		return stringBuilder.ToString();
	}

	private static Pix BitmapToPix(Bitmap bitmap)
	{
		using var ms = new MemoryStream();
		bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
		return Pix.LoadFromMemory(ms.ToArray());
	}

	private static Bitmap PixToBitmap(Pix pix)
	{
		string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
		try
		{
			pix.Save(tmp, Tesseract.ImageFormat.Png);
			return new Bitmap(tmp);
		}
		finally
		{
			try { File.Delete(tmp); } catch { }
		}
	}

	private void SaveBitmap(string text, Bitmap proc)
	{
		try
		{
			text = text.RemoveIllegalCharacters();
			if (!Directory.Exists("UserData\\Captchas"))
			{
				Directory.CreateDirectory("UserData\\Captchas");
			}
			System.Drawing.Imaging.ImageFormat imageFormat = (OriginalImage ?? proc).GetImageFormat();
			if (imageFormat != System.Drawing.Imaging.ImageFormat.Png)
			{
				OriginalImage?.Save("UserData\\Captchas\\Original (" + text + ").Png");
				proc.Save("UserData\\Captchas\\Processed (" + text + ").Png");
			}
			OriginalImage?.Save($"UserData\\Captchas\\Original ({text}).{imageFormat}");
			proc.Save($"UserData\\Captchas\\Processed ({text}).{imageFormat}");
		}
		catch (Exception)
		{
		}
	}

	public Bitmap ApplyFilters(Bitmap bitmap, BotData data = null)
	{
		if (Filters.Count <= 0)
		{
			return bitmap;
		}
		Bitmap newBmp = null;
		Task task = Task.Run(delegate
		{
			using ImageFactory imageFactory = new ImageFactory();
			imageFactory.Load(bitmap.Clone() as Bitmap);
			List<CaptchaFilter> filters = GetFilters(imageFactory);
			for (int i = 0; i < filters.Count; i++)
			{
				if (filters[i] != null)
				{
					if (InvokeFilter(filters[i], imageFactory, data) == null)
					{
						i--;
					}
					else
					{
						newBmp = imageFactory.Bitmap.Clone() as Bitmap;
					}
				}
			}
		});
		task.Wait();
		task.Dispose();
		return newBmp ?? bitmap;
	}

	private object InvokeFilter(CaptchaFilter filter, object obj = null, BotData data = null)
	{
		Bitmap bitmap = (obj as ImageFactory).Image as Bitmap;
		if (filter.ParameterType == null)
		{
			if (filter.Method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Bitmap))
			{
				MethodInfo method = filter.Method;
				object[] parameters = new Bitmap[1] { bitmap };
				return method.Invoke(null, parameters);
			}
			return filter.Method.Invoke(obj, null);
		}
		List<object> list = new List<object>();
		object[] values = ((!(filter.Parameter is object[] array)) ? new object[1] { filter.Parameter } : array);
		ParameterInfo[] parameters2 = filter.Method.GetParameters();
		for (int i = 0; i < parameters2.Length; i++)
		{
			Type parameterType = parameters2[i].ParameterType;
			if (parameterType == typeof(Bitmap))
			{
				list.Add(bitmap);
			}
			else if (parameters2.Length != 0 && values.Length != 0)
			{
				list.Add(FilterParser.ParseObject(ref values, parameterType));
			}
		}
		try
		{
			return filter.Method.Invoke(obj, list.ToArray());
		}
		catch (TargetInvocationException ex)
		{
			data?.Log(new LogEntry("[" + filter.Name + "]: " + ex.InnerException.Message, Colors.Red));
			return filter.Method.Invoke(null, list.ToArray());
		}
	}

	private List<CaptchaFilter> GetFilters(ImageFactory imageFactory)
	{
		List<CaptchaFilter> list = new List<CaptchaFilter>();
		Filters = Filters.Where((ImageFilter f) => !f.Name.StartsWith("!")).ToList();
		int count = Filters.Count;
		for (int i = 0; i < count; i++)
		{
			ImageFilter filter = Filters.ToList()[i];
			object objectType = GetObjectType(filter.Value);
			(string, string, Type) tuple;
			try
			{
				tuple = Processors.First(((string, string, Type) p) => p.Item1 == filter.Name);
			}
			catch (InvalidOperationException)
			{
				continue;
			}
			if (tuple.Item1 == null || tuple.Item2 == null)
			{
				continue;
			}
			if (tuple.Item3 != null)
			{
				MethodInfo method = imageFactory.GetType().GetMethod(tuple.Item2, new Type[1] { tuple.Item3 });
				if (method == null)
				{
					try
					{
						method = imageFactory.GetType().GetMethod(tuple.Item2);
					}
					catch
					{
					}
				}
				if (method != null)
				{
					CaptchaFilter obj2 = new CaptchaFilter
					{
						Method = method,
						Parameter = objectType,
						ParameterType = tuple.Item3
					};
					(obj2.Name, _, _) = tuple;
					list.Add(obj2);
				}
			}
			else
			{
				MethodInfo method2 = imageFactory.GetType().GetMethod(tuple.Item2);
				if (method2 != null)
				{
					CaptchaFilter obj3 = new CaptchaFilter
					{
						Method = method2,
						Parameter = objectType,
						ParameterType = tuple.Item3
					};
					(obj3.Name, _, _) = tuple;
					list.Add(obj3);
				}
			}
		}
		return list;
	}

	private object GetObjectType(string value)
	{
		if (string.IsNullOrWhiteSpace(value) || value.ToLower() == "null")
		{
			return null;
		}
		if (value.Contains(","))
		{
			return ((IEnumerable<object>)value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
		}
		return value;
	}

	public Bitmap Base64ImageDecoder(string input)
	{
		byte[] array = Convert.FromBase64String(FixBase64ForImage(input));
		using MemoryStream stream = new MemoryStream(array, 0, array.Length);
		return (Bitmap)Image.FromStream(stream);
	}

	public CProxy CreateProxy(string value, ExProxyType proxyType)
	{
		string proxy = string.Empty;
		string text = string.Empty;
		string text2 = string.Empty;
		if (value.Contains(":"))
		{
			string[] array = value.Split(':');
			proxy = array[0];
			try
			{
				text = array[1];
			}
			catch
			{
			}
			try
			{
				text2 = array[2];
			}
			catch
			{
			}
		}
		CProxy cProxy = new CProxy(proxy, proxyType);
		if (!string.IsNullOrEmpty(text))
		{
			cProxy.Username = text;
		}
		if (!string.IsNullOrEmpty(text2))
		{
			cProxy.Password = text2;
		}
		return cProxy;
	}
}

