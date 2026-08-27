using System;
using OpenBullet.Views.StackerBlocks;
using RuriLib.Functions.Conditions;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class KeyViewModel : ViewModelBase
{
	private KeyFullId id;

	private Key _key;

	public KeyFullId Id
	{
		get
		{
			return id;
		}
		set
		{
			if (!object.Equals(id, value))
			{
				id = value;
				OnPropertyChanged("Id");
			}
		}
	}

	public Key Key
	{
		get
		{
			return _key;
		}
		set
		{
			if (!object.Equals(_key, value))
			{
				_key = value;
				OnPropertyChanged("LeftTerm");
				OnPropertyChanged("Comparer");
				OnPropertyChanged("RightTerm");
				OnPropertyChanged("Key");
			}
		}
	}

	public string LeftTerm
	{
		get
		{
			return Key.LeftTerm;
		}
		set
		{
			if (!string.Equals(LeftTerm, value, StringComparison.Ordinal))
			{
				Key.LeftTerm = value;
				OnPropertyChanged("LeftTerm");
			}
		}
	}

	public Comparer Comparer
	{
		get
		{
			return Key.Comparer;
		}
		set
		{
			if (Comparer != value)
			{
				Key.Comparer = value;
				OnPropertyChanged("Comparer");
			}
		}
	}

	public string RightTerm
	{
		get
		{
			return Key.RightTerm;
		}
		set
		{
			if (!string.Equals(RightTerm, value, StringComparison.Ordinal))
			{
				Key.RightTerm = value;
				OnPropertyChanged("RightTerm");
			}
		}
	}

	public KeyViewModel(Key key, int id, int parentId)
	{
		Key = key;
		Id = new KeyFullId
		{
			KeyId = id,
			ParentId = parentId
		};
		OnPropertyChanged("FullId");
		OnPropertyChanged("Id");
	}
}
