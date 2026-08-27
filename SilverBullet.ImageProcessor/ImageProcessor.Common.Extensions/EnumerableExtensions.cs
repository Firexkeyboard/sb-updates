using System;
using System.Collections.Generic;

namespace ImageProcessor.Common.Extensions;

public static class EnumerableExtensions
{
	public static IEnumerable<int> SteppedRange(this int fromInclusive, int toExclusive, int step)
	{
		long num = (long)(fromInclusive + toExclusive) - 1L;
		if (toExclusive < 0 || num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("toExclusive");
		}
		return RangeIterator(fromInclusive, (int i) => i < toExclusive, step);
	}

	public static IEnumerable<int> SteppedRange(this int fromInclusive, Func<int, bool> toDelegate, int step)
	{
		return RangeIterator(fromInclusive, toDelegate, step);
	}

	private static IEnumerable<int> RangeIterator(int fromInclusive, Func<int, bool> toDelegate, int step)
	{
		for (int i = fromInclusive; toDelegate(i); i += step)
		{
			yield return i;
		}
	}
}
