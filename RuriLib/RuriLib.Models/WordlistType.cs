using System.Collections.Generic;

namespace RuriLib.Models;

public class WordlistType
{
	public bool Verify { get; set; }

	public string Regex { get; set; } = "^.*$";

	public string Name { get; set; } = "Default";

	public string Separator { get; set; } = ":";

	public List<string> Slices { get; set; } = new List<string>();
}
