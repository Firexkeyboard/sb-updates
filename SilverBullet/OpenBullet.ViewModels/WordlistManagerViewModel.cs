using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using OpenBullet.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class WordlistManagerViewModel : ViewModelBase, IWordlistManager
{
	private LiteDBRepository<Wordlist> _repo;

	private ObservableCollection<Wordlist> wordlistsCollection;

	private string searchString = "";

	public ObservableCollection<Wordlist> WordlistsCollection
	{
		get
		{
			return wordlistsCollection;
		}
		private set
		{
			if (!object.Equals(wordlistsCollection, value))
			{
				wordlistsCollection = value;
				OnPropertyChanged("Total");
				OnPropertyChanged("Wordlists");
				OnPropertyChanged("WordlistsCollection");
			}
		}
	}

	public int Total => WordlistsCollection.Count;

	public IEnumerable<Wordlist> Wordlists => WordlistsCollection;

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
				CollectionViewSource.GetDefaultView(WordlistsCollection).Refresh();
				OnPropertyChanged("Total");
			}
		}
	}

	public WordlistManagerViewModel()
	{
		_repo = new LiteDBRepository<Wordlist>(SB.dataBaseFile, "wordlists");
		WordlistsCollection = new ObservableCollection<Wordlist>();
		RefreshList();
	}

	public void HookFilters()
	{
		((CollectionView)CollectionViewSource.GetDefaultView(WordlistsCollection)).Filter = WordlistsFilter;
	}

	private bool WordlistsFilter(object item)
	{
		return ((Wordlist)((item is Wordlist) ? item : null)).Name.ToLower().Contains(searchString.ToLower());
	}

	public Wordlist GetWordlistByName(string name)
	{
		return WordlistsCollection.FirstOrDefault((Wordlist x) => x.Name == name);
	}

	public static Wordlist FileToWordlist(string path)
	{
		Wordlist val = new Wordlist(Path.GetFileNameWithoutExtension(path), path, SB.Settings.Environment.WordlistTypes.First().Name, "", true, false, (SubWordlist[])null);
		string text = File.ReadLines(val.Path).First();
		val.Type = SB.Settings.Environment.RecognizeWordlistType(text);
		return val;
	}

	public void Add(Wordlist wordlist)
	{
		if (WordlistsCollection.Any((Wordlist w) => w.Path == wordlist.Path))
		{
			throw new Exception("Wordlist already present: " + wordlist.Path);
		}
		WordlistsCollection.Add(wordlist);
		_repo.Add(wordlist);
	}

	public void RefreshList()
	{
		WordlistsCollection = new ObservableCollection<Wordlist>(_repo.Get());
		HookFilters();
	}

	public void Update(Wordlist wordlist)
	{
		_repo.Update(wordlist);
	}

	public void Remove(Wordlist wordlist)
	{
		WordlistsCollection.Remove(wordlist);
		_repo.Remove(wordlist);
	}

	public void RemoveAll()
	{
		WordlistsCollection.Clear();
		_repo.RemoveAll();
	}

	public void DeleteNotFound()
	{
		for (int i = 0; i < WordlistsCollection.Count; i++)
		{
			Wordlist val = WordlistsCollection[i];
			if (!File.Exists(val.Path))
			{
				Remove(val);
				i--;
			}
		}
	}
}
