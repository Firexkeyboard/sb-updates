using System;
using System.Collections.Generic;

namespace RuriLib;

public static class ObjectExtensions
{
	public static T[] Remove<T>(this T[] original, T itemToRemove)
	{
		int num = Array.IndexOf(original, itemToRemove);
		if (num == -1)
		{
			return original;
		}
		List<T> list = new List<T>(original);
		list.RemoveAt(num);
		return list.ToArray();
	}

	public static T[] RemoveAt<T>(this T[] source, int index)
	{
		T[] array = new T[source.Length - 1];
		if (index > 0)
		{
			Array.Copy(source, 0, array, 0, index);
		}
		if (index < source.Length - 1)
		{
			Array.Copy(source, index + 1, array, index, source.Length - index - 1);
		}
		return array;
	}
}
