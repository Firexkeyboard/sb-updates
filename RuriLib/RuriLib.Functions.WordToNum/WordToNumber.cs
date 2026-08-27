using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.Functions.WordToNum;

public class WordToNumber
{
	private static Dictionary<string, long> numberTable = new Dictionary<string, long>
	{
		{ "zero", 0L },
		{ "one", 1L },
		{ "two", 2L },
		{ "three", 3L },
		{ "four", 4L },
		{ "five", 5L },
		{ "six", 6L },
		{ "seven", 7L },
		{ "eight", 8L },
		{ "nine", 9L },
		{ "ten", 10L },
		{ "eleven", 11L },
		{ "twelve", 12L },
		{ "thirteen", 13L },
		{ "fourteen", 14L },
		{ "fifteen", 15L },
		{ "sixteen", 16L },
		{ "seventeen", 17L },
		{ "eighteen", 18L },
		{ "nineteen", 19L },
		{ "twenty", 20L },
		{ "thirty", 30L },
		{ "forty", 40L },
		{ "fifty", 50L },
		{ "sixty", 60L },
		{ "seventy", 70L },
		{ "eighty", 80L },
		{ "ninety", 90L },
		{ "hundred", 100L },
		{ "thousand", 1000L },
		{ "million", 1000000L },
		{ "billion", 1000000000L },
		{ "trillion", 1000000000000L },
		{ "quadrillion", 1000000000000000L },
		{ "quintillion", 1000000000000000000L }
	};

	public static long ToLong(string numberString)
	{
		IEnumerable<long> enumerable = from Match m in Regex.Matches(numberString, "\\w+")
			select m.Value.ToLowerInvariant() into v
			where numberTable.ContainsKey(v)
			select numberTable[v];
		long num = 0L;
		long num2 = 0L;
		foreach (long item in enumerable)
		{
			if (item >= 1000)
			{
				num2 += num * item;
				num = 0L;
			}
			else
			{
				num = ((item < 100) ? (num + item) : (num * item));
			}
		}
		return (num2 + num) * ((!numberString.StartsWith("minus", StringComparison.InvariantCultureIgnoreCase)) ? 1 : (-1));
	}
}
