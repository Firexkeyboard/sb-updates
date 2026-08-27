using System.Collections.Generic;
using RuriLib.Models;

namespace RuriLib.Interfaces;

public interface IHitsDB
{
	IEnumerable<Hit> Hits { get; }

	void DeleteDuplicates();

	void RemoveAll();

	void Remove(Hit hit);

	void Remove(IEnumerable<Hit> hits);

	void Update(Hit hit);

	void Add(Hit hit);
}
