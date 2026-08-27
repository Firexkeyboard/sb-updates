using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;

namespace RuriLib.Utils.Parsing;

public static class Parse
{
	public static IEnumerable<string> LR(string input, string left, string right, bool recursive = false, bool useRegex = false)
	{
		if (left == string.Empty && right == string.Empty)
		{
			return new string[1] { input };
		}
		if ((left != string.Empty && !input.Contains(left)) || (right != string.Empty && !input.Contains(right)))
		{
			return new string[0];
		}
		string text = input;
		int num = 0;
		int num2 = 0;
		List<string> list = new List<string>();
		if (recursive)
		{
			if (useRegex)
			{
				try
				{
					string pattern = BuildLRPattern(left, right);
					foreach (Match item in Regex.Matches(text, pattern))
					{
						list.Add(item.Value);
					}
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					while (left == string.Empty || (text.Contains(left) && (right == string.Empty || text.Contains(right))))
					{
						num = ((!(left == string.Empty)) ? (text.IndexOf(left) + left.Length) : 0);
						text = text.Substring(num);
						num2 = ((right == string.Empty) ? (text.Length - 1) : text.IndexOf(right));
						string text2 = text.Substring(0, num2);
						list.Add(text2);
						text = text.Substring(text2.Length + right.Length);
					}
				}
				catch
				{
				}
			}
		}
		else if (useRegex)
		{
			string pattern2 = BuildLRPattern(left, right);
			MatchCollection matchCollection = Regex.Matches(text, pattern2);
			if (matchCollection.Count > 0)
			{
				list.Add(matchCollection[0].Value);
			}
		}
		else
		{
			try
			{
				num = ((!(left == string.Empty)) ? (text.IndexOf(left) + left.Length) : 0);
				text = text.Substring(num);
				num2 = ((right == string.Empty) ? text.Length : text.IndexOf(right));
				list.Add(text.Substring(0, num2));
			}
			catch
			{
			}
		}
		return list;
	}

	public static IEnumerable<string> CSS(string input, string selector, string attribute, int index = 0, bool recursive = false)
	{
		HtmlParser htmlParser = new HtmlParser();
		IHtmlDocument htmlDocument = null;
		htmlDocument = htmlParser.ParseDocument(input);
		List<string> list = new List<string>();
		if (recursive)
		{
			foreach (IElement item in htmlDocument.QuerySelectorAll(selector))
			{
				if (!(attribute == "innerHTML"))
				{
					if (attribute == "outerHTML")
					{
						list.Add(item.OuterHtml);
						continue;
					}
					foreach (IAttr attribute2 in item.Attributes)
					{
						if (attribute2.Name == attribute)
						{
							list.Add(attribute2.Value);
							break;
						}
					}
				}
				else
				{
					list.Add(item.InnerHtml);
				}
			}
		}
		else if (!(attribute == "innerHTML"))
		{
			if (attribute == "outerHTML")
			{
				list.Add(htmlDocument.QuerySelectorAll(selector)[index].OuterHtml);
			}
			else
			{
				foreach (IAttr attribute3 in htmlDocument.QuerySelectorAll(selector)[index].Attributes)
				{
					if (attribute3.Name == attribute)
					{
						list.Add(attribute3.Value);
						break;
					}
				}
			}
		}
		else
		{
			list.Add(htmlDocument.QuerySelectorAll(selector)[index].InnerHtml);
		}
		return list;
	}

	public static IEnumerable<string> JSON(string input, string field, bool recursive = false, bool useJToken = false)
	{
		List<string> list = new List<string>();
		if (useJToken)
		{
			if (recursive)
			{
				if (input.Trim().StartsWith("["))
				{
					foreach (JToken item in JArray.Parse(input).SelectTokens(field, errorWhenNoMatch: false))
					{
						list.Add(item.ToString());
					}
				}
				else
				{
					foreach (JToken item2 in JObject.Parse(input).SelectTokens(field, errorWhenNoMatch: false))
					{
						list.Add(item2.ToString());
					}
				}
			}
			else if (input.Trim().StartsWith("["))
			{
				JArray jArray = JArray.Parse(input);
				var _tok = jArray.SelectToken(field, errorWhenNoMatch: false);
				if (_tok != null) list.Add(_tok.ToString());
			}
			else
			{
				JObject jObject = JObject.Parse(input);
				var _tok = jObject.SelectToken(field, errorWhenNoMatch: false);
				if (_tok != null) list.Add(_tok.ToString());
			}
		}
		else
		{
			List<KeyValuePair<string, string>> list2 = new List<KeyValuePair<string, string>>();
			parseJSON("", input, list2);
			foreach (KeyValuePair<string, string> item3 in list2)
			{
				if (item3.Key == field)
				{
					list.Add(item3.Value);
				}
			}
			if (!recursive && list.Count > 1)
			{
				list = new List<string> { list.First() };
			}
		}
		return list;
	}

	public static IEnumerable<string> REGEX(string input, string pattern, string output, bool recursive = false, RegexOptions? options = null)
	{
		List<string> list = new List<string>();
		if (!options.HasValue)
		{
			options = RegexOptions.None;
		}
		if (recursive)
		{
			foreach (Match item in Regex.Matches(input, pattern, options.Value))
			{
				string text = output;
				for (int i = 0; i < item.Groups.Count; i++)
				{
					text = text.Replace("[" + i + "]", item.Groups[i].Value);
				}
				list.Add(text);
			}
		}
		else
		{
			Match match2 = Regex.Match(input, pattern, options.Value);
			if (match2.Success)
			{
				string text2 = output;
				for (int j = 0; j < match2.Groups.Count; j++)
				{
					text2 = text2.Replace("[" + j + "]", match2.Groups[j].Value);
				}
				list.Add(text2);
			}
		}
		return list;
	}

	private static string BuildLRPattern(string ls, string rs)
	{
		string text = (string.IsNullOrEmpty(ls) ? "^" : Regex.Escape(ls));
		string text2 = (string.IsNullOrEmpty(rs) ? "$" : Regex.Escape(rs));
		return "(?<=" + text + ").+?(?=" + text2 + ")";
	}

	private static void parseJSON(string A, string B, List<KeyValuePair<string, string>> jsonlist)
	{
		jsonlist.Add(new KeyValuePair<string, string>(A, B));
		if (B.StartsWith("["))
		{
			JArray jArray = null;
			try
			{
				jArray = JArray.Parse(B);
			}
			catch
			{
				return;
			}
			foreach (JToken item in jArray.Children())
			{
				parseJSON("", item.ToString(), jsonlist);
			}
		}
		if (!B.Contains("{"))
		{
			return;
		}
		JObject jObject = null;
		try
		{
			jObject = JObject.Parse(B);
		}
		catch
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item2 in jObject)
		{
			parseJSON(item2.Key, item2.Value.ToString(), jsonlist);
		}
	}
}
