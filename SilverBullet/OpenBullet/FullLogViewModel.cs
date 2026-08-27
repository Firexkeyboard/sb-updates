using System;
using System.Collections.Generic;
using RuriLib.ViewModels;

namespace OpenBullet;

public class FullLogViewModel : ViewModelBase
{
	private string searchString = "";

	private List<int> indexes = new List<int>();

	private int currentSearchMatch;

	public string SearchString
	{
		get
		{
			return searchString;
		}
		set
		{
			if (!string.Equals(searchString, value, StringComparison.Ordinal))
			{
				searchString = value;
				OnPropertyChanged("SearchString");
				OnPropertyChanged("SearchProgress");
			}
		}
	}

	public List<int> Indexes
	{
		get
		{
			return indexes;
		}
		set
		{
			if (!object.Equals(indexes, value))
			{
				indexes = value;
				OnPropertyChanged("Indexes");
				OnPropertyChanged("TotalSearchMatches");
				OnPropertyChanged("CurrentSearchMatch");
			}
		}
	}

	public int TotalSearchMatches => Indexes.Count;

	public int CurrentSearchMatch
	{
		get
		{
			return currentSearchMatch;
		}
		set
		{
			if (currentSearchMatch != value)
			{
				currentSearchMatch = value;
				OnPropertyChanged("CurrentSearchMatch");
			}
		}
	}

	public void UpdateTotalSearchMatches()
	{
		OnPropertyChanged("TotalSearchMatches");
	}
}
