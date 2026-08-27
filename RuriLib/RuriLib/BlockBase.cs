using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Newtonsoft.Json;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace RuriLib;

public abstract class BlockBase : ViewModelBase
{
	private string label = "BASE";

	private bool disabled;

	public string Label
	{
		get
		{
			return label;
		}
		set
		{
			label = value;
			OnPropertyChanged("Label");
		}
	}

	public bool Disabled
	{
		get
		{
			return disabled;
		}
		set
		{
			disabled = value;
			OnPropertyChanged("Disabled");
		}
	}

	[JsonIgnore]
	public bool IsSelenium => GetType().ToString().StartsWith("S");

	[JsonIgnore]
#pragma warning disable CS0612
	public bool IsCaptcha
	{
		get
		{
			if (!(GetType() == typeof(BlockImageCaptcha)))
			{
				return GetType() == typeof(BlockRecaptcha);
			}
			return true;
		}
	}
#pragma warning restore CS0612

	public virtual BlockBase FromLS(string line)
	{
		throw new Exception("Cannot Convert to the abstract class BlockBase");
	}

	public virtual BlockBase FromLS(List<string> lines)
	{
		throw new Exception("Cannot Convert from the abstract class BlockBase");
	}

	public virtual string ToLS(bool indent = true)
	{
		throw new Exception("Cannot Convert from the abstract class BlockBase");
	}

	public virtual void Process(BotData data)
	{
		// LogBuffer is cleared by TakeStep (LoliScript) or once before the full Roslyn
		// script (LoliCode). Clearing here would erase previous blocks' log output when
		// multiple blocks run inside a single TakeStep (LoliCode mode).
		data.Log(new LogEntry($"<--- Executing Block {Label} --->", Colors.Orange));
	}

	public static List<string> ReplaceValuesRecursive(string input, BotData data)
	{
		List<string> list = new List<string>();
		MatchCollection matchCollection = Regex.Matches(input, "<([^\\[]*)\\[\\*\\]>");
		List<CVar> list2 = new List<CVar>();
		foreach (Match item in matchCollection)
		{
			string value = item.Groups[1].Value;
			CVar cVar = data.Variables.Get(value);
			if (cVar == null)
			{
				cVar = data.GlobalVariables.Get(value);
				if (cVar == null)
				{
					continue;
				}
			}
			if (cVar.Type == CVar.VarType.List)
			{
				list2.Add(cVar);
			}
		}
		if (list2.Count > 0)
		{
			dynamic val = list2.OrderBy((CVar v) => v.Value.Count).Last().Value.Count;
			for (int j = 0; j < val; j++)
			{
				string text = input;
				foreach (CVar item2 in list2)
				{
					List<string> list3 = (List<string>)item2.Value;
					text = ((list3.Count <= j) ? text.Replace("<" + item2.Name + "[*]>", "NULL") : text.Replace("<" + item2.Name + "[*]>", list3[j]));
				}
				list.Add(text);
			}
		}
		else
		{
			Match match = Regex.Match(input, "<([^\\(]*)\\(\\*\\)>");
			if (match.Success)
			{
				string value2 = match.Groups[0].Value;
				string value3 = match.Groups[1].Value;
				Dictionary<string, string> dictionary = data.Variables.GetDictionary(value3);
				if (dictionary == null)
				{
					dictionary = data.GlobalVariables.GetDictionary(value3);
				}
				if (dictionary == null)
				{
					list.Add(input);
				}
				else
				{
					foreach (KeyValuePair<string, string> item3 in dictionary)
					{
						list.Add(input.Replace(value2, item3.Value));
					}
				}
			}
			else
			{
				match = Regex.Match(input, "<([^\\{]*)\\{\\*\\}>");
				if (match.Success)
				{
					string value4 = match.Groups[0].Value;
					string value5 = match.Groups[1].Value;
					Dictionary<string, string> dictionary2 = data.Variables.GetDictionary(value5);
					if (dictionary2 == null)
					{
						dictionary2 = data.GlobalVariables.GetDictionary(value5);
					}
					if (dictionary2 == null)
					{
						list.Add(input);
					}
					else
					{
						foreach (KeyValuePair<string, string> item4 in dictionary2)
						{
							list.Add(input.Replace(value4, item4.Key));
						}
					}
				}
				else
				{
					list.Add(input);
				}
			}
		}
		return list.Select((string i) => ReplaceValues(i, data)).ToList();
	}

	public static string ReplaceValues(string input, BotData data)
	{
		if (input == null) return string.Empty;
		// Resolve OB2 @VAR syntax before the <VAR> pass.
		// @input.USER / @data.SOURCE → convert to <USER> / <SOURCE> so existing logic handles them.
		// @VARNAME → resolve from bot variables; keep literal "@VARNAME" if the variable is undefined,
		// so @TOMATE stays as "@TOMATE" when TOMATE hasn't been set (no silent data loss).
		if (input.Contains("@"))
		{
			// (?<!<) ensures we don't touch @VAR that's already inside <@VAR> angle-bracket form.
			input = Regex.Replace(input, @"(?<!<)@(?:input|data)\.([A-Za-z0-9_]+)",
				m => "<" + m.Groups[1].Value.ToUpperInvariant() + ">");
			// Negative lookbehind: skip @VAR when preceded by alphanumeric/./:/@ (email, URL, etc.)
			// Only match @VAR at word-boundaries like start-of-string, space, quote, comma, (
			input = Regex.Replace(input, @"(?<![A-Za-z0-9_.:/\\@])@([A-Za-z0-9_]+)", m =>
			{
				string name = m.Groups[1].Value;
				CVar cvar = data.Variables.Get(name) ?? data.GlobalVariables?.Get(name);
				if (cvar == null) return m.Value; // undefined → keep literal
				return cvar.Value is List<string> lst ? string.Join(", ", lst) : cvar.Value?.ToString() ?? "";
			});
		}
		if (!input.Contains("<") && !input.Contains(">"))
		{
			return input;
		}
		// Normalize OB2-style refs: <input.USER> → <USER>, <data.SOURCE> → <SOURCE>
		if (input.Contains("<input.") || input.Contains("<data."))
			input = Regex.Replace(input, @"<(input|data)\.([A-Za-z0-9_]+)>",
				m => "<" + m.Groups[2].Value.ToUpperInvariant() + ">");
		// Allow <USERNAME>/<PASSWORD> as aliases for <USER>/<PASS>
		if (input.Contains("<USERNAME>")) input = input.Replace("<USERNAME>", "<USER>");
		if (input.Contains("<PASSWORD>")) input = input.Replace("<PASSWORD>", "<PASS>");
		string text = "";
		string text2 = input;
		var _seen = new HashSet<string> { text2 };
		do
		{
			text = text2;
			text2 = text2.Replace("<INPUT>", data.Data.Data);
			// OB2 compat: <DATA> / <LINE> → full raw wordlist line, but only when no
			// WordlistType slice with that name exists (a slice CVar takes precedence).
			if (text2.Contains("<DATA>") && data.Variables.Get("DATA") == null)
				text2 = text2.Replace("<DATA>", data.Data.Data);
			if (text2.Contains("<LINE>") && data.Variables.Get("LINE") == null)
				text2 = text2.Replace("<LINE>", data.Data.Data);
			text2 = text2.Replace("<STATUS>", data.Status.ToString());
			text2 = text2.Replace("<BOTNUM>", data.BotNumber.ToString());
			text2 = text2.Replace("<RETRIES>", data.Data.Retries.ToString());
			text2 = text2.Replace("<OCRRATE>", data.OcrRate.ToString());
			text2 = text2.Replace("<BASEDIR>", AppDomain.CurrentDomain.BaseDirectory);
			if (data.Proxy != null)
			{
				text2 = text2.Replace("<PROXY>", data.Proxy.ToString());
				text2 = text2.Replace("<CUSTOMPROXY>", data.Proxy.GetCustomProxy());
				text2 = text2.Replace("<PROXYTYPE>", data.Proxy.Type.ToString().ToLower());
			}
			MatchCollection matchCollection = Regex.Matches(text2, "<([^<>]*)>");
			int count = matchCollection.Count;
			for (int i = 0; i < count; i++)
			{
				Match match = matchCollection[i];
				string value = match.Groups[0].Value;
				string value2 = match.Groups[1].Value;
				string value3 = Regex.Match(value2, "^[^\\[\\{\\(]*").Value;
				string lookupName = value3.TrimStart('@');
				string text3 = value2.Substring(value3.Length);
				CVar cVar = data.Variables.Get(value3);
				if (cVar == null && lookupName != value3)
					cVar = data.Variables.Get(lookupName);
				if (cVar == null)
				{
					cVar = data.GlobalVariables.Get(value3);
					if (cVar == null && lookupName != value3)
						cVar = data.GlobalVariables.Get(lookupName);
				}
				if (cVar == null)
				{
					// Built-in dictionary lookups: <HEADERS(key)> and <COOKIES(key)>
					// are stored on BotData fields, not as CVars.
					if (text3.Length > 0 && text3.Contains("(") && text3.Contains(")"))
					{
						string dictKey = ParseArguments(text3, '(', ')').FirstOrDefault() ?? "";
						if (value3.Equals("HEADERS", StringComparison.OrdinalIgnoreCase) && data.ResponseHeaders != null)
						{
							string hdrVal = data.ResponseHeaders
								.FirstOrDefault(h => h.Key.Equals(dictKey, StringComparison.OrdinalIgnoreCase)).Value ?? "";
							text2 = text2.Replace(value, hdrVal);
						}
						else if (value3.Equals("COOKIES", StringComparison.OrdinalIgnoreCase) && data.Cookies != null)
						{
							string cookieVal = data.Cookies
								.FirstOrDefault(h => h.Key.Equals(dictKey, StringComparison.OrdinalIgnoreCase)).Value ?? "";
							text2 = text2.Replace(value, cookieVal);
						}
					}
					continue;
				}
				switch (cVar.Type)
				{
				case CVar.VarType.Single:
					text2 = text2.Replace(value, cVar.Value);
					break;
				case CVar.VarType.List:
				{
					if (string.IsNullOrEmpty(text3))
					{
						text2 = text2.Replace(value, cVar.ToString());
						break;
					}
					int result = 0;
					int.TryParse(ParseArguments(text3, '[', ']')[0], out result);
					string listItem = cVar.GetListItem(result);
					if (listItem != null)
					{
						text2 = text2.Replace(value, listItem);
					}
					break;
				}
				case CVar.VarType.Dictionary:
					if (text3.Contains("(") && text3.Contains(")"))
					{
						string key = ParseArguments(text3, '(', ')')[0];
						try
						{
							text2 = text2.Replace(value, cVar.GetDictValue(key));
						}
						catch
						{
						}
					}
					else if (text3.Contains("{") && text3.Contains("}"))
					{
						string value4 = ParseArguments(text3, '{', '}')[0];
						try
						{
							text2 = text2.Replace(value, cVar.GetDictKey(value4));
						}
						catch
						{
						}
					}
					else
					{
						text2 = text2.Replace(value, cVar.ToString());
					}
					break;
				}
			}
		}
		while (input.Contains("<") && input.Contains(">") && text2 != text && _seen.Add(text2));
		return text2;
	}

	public static List<string> ParseArguments(string input, char delimL, char delimR)
	{
		List<string> list = new List<string>();
		MatchCollection source = Regex.Matches(input, "\\" + delimL + "([^\\" + delimR + "]*)\\" + delimR);
		list.AddRange(from Match m in source
			select m.Groups[1].Value);
		return list;
	}

	public static void UpdateSeleniumData(BotData data)
	{
		data.Address = data.Driver.Url;
		data.ResponseSource = data.Driver.PageSource;
	}

	public static void InsertVariable(BotData data, bool isCapture, string value, string variableName, string prefix = "", string suffix = "", bool urlEncode = false, bool createEmpty = true)
	{
		InsertVariable(data, isCapture, recursive: false, new string[1] { value }, variableName, prefix, suffix, urlEncode, createEmpty);
	}

	public static void InsertVariable(BotData data, bool isCapture, IEnumerable<string> values, string variableName, string prefix = "", string suffix = "", bool urlEncode = false, bool createEmpty = true)
	{
		InsertVariable(data, isCapture, recursive: true, values, variableName, prefix, suffix, urlEncode, createEmpty);
	}

	internal static void InsertVariable(BotData data, bool isCapture, bool recursive, IEnumerable<string> values, string variableName, string prefix = "", string suffix = "", bool urlEncode = false, bool createEmpty = true)
	{
		List<string> list = values.Select((string v) => ReplaceValues(prefix, data) + v.Trim() + ReplaceValues(suffix, data)).ToList();
		if (urlEncode)
		{
			list = list.Select((string v) => Uri.EscapeDataString(v)).ToList();
		}
		CVar cVar = null;
		if (recursive)
		{
			if (list.Count == 0)
			{
				if (createEmpty)
				{
					cVar = new CVar(variableName, list, isCapture);
				}
			}
			else
			{
				cVar = new CVar(variableName, list, isCapture);
			}
		}
		else if (list.Count == 0)
		{
			if (createEmpty)
			{
				cVar = new CVar(variableName, "", isCapture);
			}
		}
		else
		{
			cVar = new CVar(variableName, list.First(), isCapture);
		}
		if (!data.ConfigSettings.SaveEmptyCaptures && isCapture && (list.Count == 0 || (list.Count > 0 && string.IsNullOrWhiteSpace(list.First()))))
		{
			cVar = null;
		}
		if (cVar != null)
		{
			data.Variables.Set(cVar);
			data.Log(new LogEntry("Parsed variable | Name: " + cVar.Name + " | Value: " + cVar.ToString() + Environment.NewLine, isCapture ? Colors.OrangeRed : Colors.Gold));
		}
		else
		{
			data.Log(new LogEntry("Could not parse any data. The variable was not created.", Colors.White));
		}
	}

	public static string TruncatePretty(string input, int max)
	{
		input = input.Replace("\r\n", "").Replace("\n", "");
		if (input.Length < max)
		{
			return input;
		}
		return input.Substring(0, max) + " [...]";
	}
}
