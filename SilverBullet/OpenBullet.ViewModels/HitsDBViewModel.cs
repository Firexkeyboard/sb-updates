using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using OpenBullet.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class HitsDBViewModel : ViewModelBase, IHitsDB
{
	public LiteDBRepository<Hit> _repo;

	private ObservableCollection<Hit> hitsCollection;

	public static readonly string defaultFilter = "All";

	private string searchString = "";

	private string typeFilter = "SUCCESS";

	private string configFilter = defaultFilter;

	public ObservableCollection<Hit> HitsCollection
	{
		get
		{
			return hitsCollection;
		}
		private set
		{
			if (!object.Equals(hitsCollection, value))
			{
				hitsCollection = value;
				OnPropertyChanged("Total");
				OnPropertyChanged("Hits");
				OnPropertyChanged("ConfigsList");
				OnPropertyChanged("Filtered");
				OnPropertyChanged("HitsCollection");
			}
		}
	}

	public int Total => HitsCollection.Count;

	public IEnumerable<Hit> Hits => HitsCollection;

	public List<string> ConfigsList => HitsCollection.Select((Hit x) => x.ConfigName).Distinct().ToList();

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
				CollectionViewSource.GetDefaultView(HitsCollection).Refresh();
				OnPropertyChanged("Filtered");
			}
		}
	}

	public string TypeFilter
	{
		get
		{
			return typeFilter;
		}
		set
		{
			if (!string.Equals(typeFilter, value, StringComparison.Ordinal))
			{
				typeFilter = value;
				OnPropertyChanged("TypeFilter");
				CollectionViewSource.GetDefaultView(HitsCollection).Refresh();
				OnPropertyChanged("Filtered");
			}
		}
	}

	public string ConfigFilter
	{
		get
		{
			return configFilter;
		}
		set
		{
			if (!string.Equals(configFilter, value, StringComparison.Ordinal))
			{
				configFilter = value;
				OnPropertyChanged("ConfigFilter");
				CollectionViewSource.GetDefaultView(HitsCollection).Refresh();
				OnPropertyChanged("Filtered");
			}
		}
	}

	public int Filtered => HitsCollection.Count((Hit h) => HitsFilter(h));

	public HitsDBViewModel()
	{
		_repo = new LiteDBRepository<Hit>(SB.dataBaseFile, "hits");
		HitsCollection = new ObservableCollection<Hit>();
		HookFilters();
	}

	public void HookFilters()
	{
		((CollectionView)CollectionViewSource.GetDefaultView(HitsCollection)).Filter = HitsFilter;
	}

	private bool HitsFilter(object item)
	{
		if (((Hit)((item is Hit) ? item : null)).Type != TypeFilter)
		{
			return false;
		}
		if (ConfigFilter != defaultFilter && ((Hit)((item is Hit) ? item : null)).ConfigName != ConfigFilter)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(SearchString))
		{
			return ((Hit)((item is Hit) ? item : null)).CapturedData.ToCaptureString().ToLower().Contains(SearchString.ToLower());
		}
		return true;
	}

	public void Add(Hit hit)
	{
		HitsCollection.Add(hit);
		_repo.Add(hit);
	}

	public void RefreshList()
	{
		HitsCollection = new ObservableCollection<Hit>(_repo.Get());
		HookFilters();
		OnPropertyChanged("Total");
	}

	public void Update(Hit hit)
	{
		_repo.Update(hit);
	}

	public void Remove(Hit hit)
	{
		HitsCollection.Remove(hit);
		_repo.Remove(hit);
	}

	public void Remove(IEnumerable<Hit> hits)
	{
		Hit[] array = hits.ToArray();
		Hit[] array2 = array;
		foreach (Hit item in array2)
		{
			HitsCollection.Remove(item);
		}
		_repo.Remove((IEnumerable<Hit>)array);
	}

	public void RemoveAll()
	{
		HitsCollection.Clear();
		_repo.RemoveAll();
	}

	public void DeleteDuplicates()
	{
		List<Hit> hits = (from h in HitsCollection
			group h by h.GetHashCode(SB.SBSettings.General.IgnoreWordlistOnHitDedupe) into g
			where g.Count() > 1
			select g).SelectMany((IGrouping<int, Hit> g) => g.OrderBy((Hit h) => h.Date).Reverse().Skip(1)).ToList();
		Remove((IEnumerable<Hit>)hits);
	}

	public void DeleteFiltered()
	{
		List<Hit> hits = HitsCollection.Where((Hit h) => (string.IsNullOrEmpty(SearchString) || h.CapturedString.ToLower().Contains(SearchString.ToLower())) && (ConfigFilter == "All" || h.ConfigName == ConfigFilter) && h.Type == TypeFilter).ToList();
		Remove((IEnumerable<Hit>)hits);
	}
}
