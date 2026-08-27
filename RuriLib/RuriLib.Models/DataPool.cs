using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RuriLib.ViewModels;

namespace RuriLib.Models;

public class DataPool : ViewModelBase
{
	public IEnumerable<string> List { get; set; }

	public List<string[]> Sublists { get; set; }

	public long Size { get; set; }

	public DataPool(IEnumerable<string> list, List<string[]> subLists = null, bool globalRemoveDup = false, bool wordlistRemoveDup = false)
	{
		if (globalRemoveDup || wordlistRemoveDup)
		{
			List = list.Distinct();
		}
		else
		{
			List = list;
		}
		Size = List.Count();
		Sublists = subLists;
	}

	public DataPool(string fileName)
	{
		List = File.ReadLines(fileName);
		Size = List.Count();
	}

	public DataPool(string charSet, int length)
	{
		List = charSet.Select((char x) => x.ToString());
		for (int i = 0; i < length; i++)
		{
			List = from x in List
				from y in charSet
				select x + y;
		}
		Size = (int)Math.Pow(charSet.Length, length);
	}

	public void RemoveDuplicate()
	{
		try
		{
			IEnumerable<string> list = List;
			if (list != null && list.Count() > 0)
			{
				List = List.Distinct();
			}
		}
		catch
		{
		}
	}
}
