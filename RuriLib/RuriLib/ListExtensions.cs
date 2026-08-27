using System;
using System.Collections.Generic;

namespace RuriLib;

public static class ListExtensions
{
	public static void Shuffle<T>(this IList<T> list, Random rng)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = rng.Next(num + 1);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}
}
