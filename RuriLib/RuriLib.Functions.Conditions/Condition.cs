using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.Functions.Conditions;

public static class Condition
{
	public static bool ReplaceAndVerify(string left, Comparer comparer, string right, BotData data)
	{
		KeycheckCondition kcCond = default(KeycheckCondition);
		kcCond.Left = left;
		kcCond.Comparer = comparer;
		kcCond.Right = right;
		return ReplaceAndVerify(kcCond, data);
	}

	public static bool ReplaceAndVerify(KeycheckCondition kcCond, BotData data)
	{
		NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
		CultureInfo provider = new CultureInfo("en-US");
		List<string> source = BlockBase.ReplaceValuesRecursive(kcCond.Left, data);
		string r = BlockBase.ReplaceValues(kcCond.Right, data);
		return kcCond.Comparer switch
		{
			Comparer.EqualTo => source.Any((string l) => l == r), 
			Comparer.NotEqualTo => source.Any((string l) => l != r), 
			Comparer.GreaterThan => source.Any((string l) => decimal.Parse(l.Replace(',', '.'), style, provider) > decimal.Parse(r.Replace(',', '.'), style, provider)), 
			Comparer.GreaterOrEqual => source.Any((string l) => decimal.Parse(l.Replace(',', '.'), style, provider) >= decimal.Parse(r.Replace(',', '.'), style, provider)), 
			Comparer.LessThan => source.Any((string l) => decimal.Parse(l.Replace(',', '.'), style, provider) < decimal.Parse(r.Replace(',', '.'), style, provider)), 
			Comparer.LessThanOrEqual => source.Any((string l) => decimal.Parse(l.Replace(',', '.'), style, provider) <= decimal.Parse(r.Replace(',', '.'), style, provider)), 
			Comparer.Contains => source.Any((string l) => l.Contains(r)), 
			Comparer.DoesNotContain => source.Any((string l) => !l.Contains(r)), 
			Comparer.Exists => source.Any((string l) => l != kcCond.Left), 
			Comparer.DoesNotExist => source.All((string l) => l == kcCond.Left), 
			Comparer.MatchesRegex => source.Any((string l) => Regex.Match(l, r).Success),
			Comparer.DoesNotMatchRegex => source.Any((string l) => !Regex.Match(l, r).Success),
			Comparer.StartsWith => source.Any((string l) => l.StartsWith(r, StringComparison.Ordinal)),
			Comparer.EndsWith => source.Any((string l) => l.EndsWith(r, StringComparison.Ordinal)),
			_ => false,
		};
	}

	public static bool Verify(KeycheckCondition kcCond)
	{
		NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
		CultureInfo provider = new CultureInfo("en-US");
		switch (kcCond.Comparer)
		{
		case Comparer.EqualTo:
			return kcCond.Left == kcCond.Right;
		case Comparer.NotEqualTo:
			return kcCond.Left != kcCond.Right;
		case Comparer.GreaterThan:
			return decimal.Parse(kcCond.Left.Replace(',', '.'), style, provider) > decimal.Parse(kcCond.Right.Replace(',', '.'), style, provider);
		case Comparer.LessThan:
			return decimal.Parse(kcCond.Left.Replace(',', '.'), style, provider) < decimal.Parse(kcCond.Right.Replace(',', '.'), style, provider);
		case Comparer.Contains:
			return kcCond.Left.Contains(kcCond.Right);
		case Comparer.DoesNotContain:
			return !kcCond.Left.Contains(kcCond.Right);
		case Comparer.Exists:
		case Comparer.DoesNotExist:
			throw new NotSupportedException("Exists and DoesNotExist operators are only supported in the ReplaceAndVerify method.");
		case Comparer.MatchesRegex:
			return Regex.Match(kcCond.Left, kcCond.Right).Success;
		case Comparer.DoesNotMatchRegex:
			return !Regex.Match(kcCond.Left, kcCond.Right).Success;
		case Comparer.StartsWith:
			return kcCond.Left.StartsWith(kcCond.Right, StringComparison.Ordinal);
		case Comparer.EndsWith:
			return kcCond.Left.EndsWith(kcCond.Right, StringComparison.Ordinal);
		default:
			return false;
		}
	}

	public static bool ReplaceAndVerifyAll(KeycheckCondition[] conditions, BotData data)
	{
		return conditions.All((KeycheckCondition c) => ReplaceAndVerify(c, data));
	}

	public static bool VerifyAll(KeycheckCondition[] conditions)
	{
		return conditions.All((KeycheckCondition c) => Verify(c));
	}

	public static bool ReplaceAndVerifyAny(KeycheckCondition[] conditions, BotData data)
	{
		return conditions.Any((KeycheckCondition c) => ReplaceAndVerify(c, data));
	}

	public static bool VerifyAny(KeycheckCondition[] conditions)
	{
		return conditions.Any((KeycheckCondition c) => Verify(c));
	}
}
