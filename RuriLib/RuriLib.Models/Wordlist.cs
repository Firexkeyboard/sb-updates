using System;
using System.IO;
using System.Linq;

namespace RuriLib.Models;

public class Wordlist : Persistable<Guid>
{
	public string Name { get; set; }

	public string Path { get; set; }

	public string Type { get; set; }

	public string Purpose { get; set; }

	public string Category { get; set; } = "";

	public long Total { get; set; }

	public bool Temporary { get; set; }

	public bool HasSubWordlist
	{
		get
		{
			if (SubWordlists != null)
			{
				return SubWordlists.Count() > 0;
			}
			return false;
		}
	}

	public bool RemoveDup { get; set; }

	public SubWordlist[] SubWordlists { get; set; }

	public Wordlist()
	{
	}

	public Wordlist(string name, string path, string type, string purpose, bool countLines = true, bool temporary = false, SubWordlist[] subwordlists = null)
	{
		Name = name;
		Path = path;
		Type = type;
		Purpose = purpose;
		Total = 0L;
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
		SubWordlists = subwordlists;
	}
}
