using System.IO;
using System.Linq;

namespace RuriLib;

public static class DirExtensions
{
	public static string[] GetFiles(string sourceFolder, string filters, SearchOption searchOption)
	{
		return filters.Split('|').SelectMany((string f) => Directory.GetFiles(sourceFolder, f, searchOption)).ToArray();
	}
}
