using System.Collections.Generic;
using RuriLib.Models;

namespace RuriLib.Interfaces;

public interface IWordlistManager
{
	IEnumerable<Wordlist> Wordlists { get; }

	void Add(Wordlist wordlist);

	void Update(Wordlist wordlist);

	void Remove(Wordlist wordlist);

	void DeleteNotFound();

	void RemoveAll();
}
