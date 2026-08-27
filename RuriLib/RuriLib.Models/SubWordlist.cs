using System.IO;
using System.Linq;

namespace RuriLib.Models;

public class SubWordlist
{
	public string Name { get; set; }

	public string Path { get; set; }

	public string Type { get; set; }

	public string Purpose { get; set; }

	public int Total { get; set; }

	public bool Temporary { get; set; }

	public SubWordlist()
	{
	}

	public SubWordlist(string name, string path, string type, string purpose, bool countLines = true, bool temporary = false)
	{
		Name = name;
		Path = path;
		Type = type;
		Purpose = purpose;
		Total = 0;
		Temporary = temporary;
		if (countLines)
		{
			try
			{
				Total = File.ReadLines(path).Count();
			}
			catch
			{
			}
		}
	}
}
