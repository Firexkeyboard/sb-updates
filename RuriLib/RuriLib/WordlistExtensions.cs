using RuriLib.Models;

namespace RuriLib;

public static class WordlistExtensions
{
	public static Wordlist Clone(this Wordlist wordlist)
	{
		return new Wordlist(wordlist.Name, wordlist.Path, wordlist.Type, wordlist.Purpose, countLines: true, wordlist.Temporary, wordlist.SubWordlists);
	}
}
