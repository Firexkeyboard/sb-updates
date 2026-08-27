using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace OpenBullet;

public static class EnumerableExtensions
{
	public static void SaveToFile<T>(this IEnumerable<T> items, string fileName, Func<T, string> mapping)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentNullException("The filename must not be empty");
		}
		File.WriteAllLines(fileName, items.Select((T i) => mapping(i)));
	}

	public static void CopyToClipboard<T>(this IEnumerable<T> items, Func<T, string> mapping)
	{
		Clipboard.SetText(string.Join(Environment.NewLine, items.Select((T i) => mapping(i))));
	}
}
