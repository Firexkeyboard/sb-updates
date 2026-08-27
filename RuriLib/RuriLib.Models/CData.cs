using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.Models;

public class CData
{
	public string Data { get; set; }

	public WordlistType Type { get; set; }

	public int Retries { get; set; }

	public bool IsValid
	{
		get
		{
			if (Type.Verify)
			{
				return Regex.Match(Data, Type.Regex).Success;
			}
			return true;
		}
	}

	public CData(string data, WordlistType type)
	{
		Data = data;
		Type = type;
	}

	public List<CVar> GetVariables(bool encode)
	{
		return (from x in Data.Split(new string[1] { Type.Separator }, StringSplitOptions.None).Zip(Type.Slices, (string k, string v) => new { k, v })
			select new CVar(x.v, encode ? Uri.EscapeDataString(x.k) : x.k, isCapture: false, hidden: true)).ToList();
	}

	// Convenience accessors for C# inline scripts that use data.Data.Username / data.Data.Password.
	// Returns the value of the first slice whose name is USER / USERNAME (or PASS / PASSWORD).
	public string Username => SliceValue("USER", "USERNAME");
	public string Password => SliceValue("PASS", "PASSWORD");

	private string SliceValue(params string[] names)
	{
		var slices = GetVariables(false);
		foreach (var n in names)
		{
			var v = slices.FirstOrDefault(s => s.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
			if (v != null) return v.Value?.ToString() ?? "";
		}
		return slices.Count > 0 ? slices[0].Value?.ToString() ?? "" : "";
	}

	public bool RespectsRules(List<DataRule> rules)
	{
		bool flag = true;
		List<CVar> variables = GetVariables(encode: false);
		foreach (DataRule rule in rules)
		{
			try
			{
				string text = (string)variables.FirstOrDefault((CVar v) => v.Name == rule.SliceName).Value;
				switch (rule.RuleType)
				{
				case RuleType.MinLength:
					flag = text.Length >= int.Parse(rule.RuleString);
					break;
				case RuleType.MaxLength:
					flag = text.Length <= int.Parse(rule.RuleString);
					break;
				case RuleType.MustContain:
					flag = CheckContains(text, rule.RuleString);
					break;
				case RuleType.MustNotContain:
					flag = !CheckContains(text, rule.RuleString);
					break;
				case RuleType.MustMatchRegex:
					flag = Regex.Match(text, rule.RuleString).Success;
					break;
				}
				if (!flag)
				{
					return false;
				}
			}
			catch
			{
			}
		}
		return true;
	}

	private static bool CheckContains(string input, string what)
	{
		switch (what)
		{
		case "Lowercase":
			return input.Any((char c) => char.IsLower(c));
		case "Uppercase":
			return input.Any((char c) => char.IsUpper(c));
		case "Digit":
			return input.Any((char c) => char.IsDigit(c));
		case "Symbol":
			return input.Any((char c) => char.IsSymbol(c) || char.IsPunctuation(c));
		default:
		{
			char[] array = what.ToCharArray();
			foreach (char value in array)
			{
				if (input.Contains(value))
				{
					return true;
				}
			}
			return false;
		}
		}
	}
}
