using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class KeychainViewModel : ViewModelBase
{
	private Random rand = new Random(3);

	private int id;

	private KeyChain _keychain;

	private ObservableCollection<KeyViewModel> _keyList = new ObservableCollection<KeyViewModel>();

	private bool _typeInitialized;

	private bool _modeInitialized;

	private bool _customTypeInitialized;

	public int Id
	{
		get
		{
			return id;
		}
		set
		{
			if (id != value)
			{
				id = value;
				OnPropertyChanged("Id");
			}
		}
	}

	public KeyChain Keychain
	{
		get
		{
			return _keychain;
		}
		set
		{
			if (!object.Equals(_keychain, value))
			{
				_keychain = value;
				OnPropertyChanged("Type");
				OnPropertyChanged("CustomVisibility");
				OnPropertyChanged("KeychainColor");
				OnPropertyChanged("Mode");
				OnPropertyChanged("CustomType");
				OnPropertyChanged("Keychain");
			}
		}
	}

	public ObservableCollection<KeyViewModel> KeyList
	{
		get
		{
			return _keyList;
		}
		set
		{
			if (!object.Equals(_keyList, value))
			{
				_keyList = value;
				OnPropertyChanged("KeyList");
			}
		}
	}

	public KeyChain.KeychainType Type
	{
		get
		{
			return Keychain.Type;
		}
		set
		{
			if (Type != value)
			{
				Keychain.Type = value;
				OnPropertyChanged("Type");
				OnPropertyChanged("KeychainColor");
				OnPropertyChanged("CustomVisibility");
			}
		}
	}

	public bool TypeInitialized
	{
		get
		{
			return _typeInitialized;
		}
		set
		{
			if (_typeInitialized != value)
			{
				_typeInitialized = value;
				OnPropertyChanged("TypeInitialized");
			}
		}
	}

	public KeyChain.KeychainMode Mode
	{
		get
		{
			return Keychain.Mode;
		}
		set
		{
			if (Mode != value)
			{
				Keychain.Mode = value;
				OnPropertyChanged("Mode");
			}
		}
	}

	public bool ModeInitialized
	{
		get
		{
			return _modeInitialized;
		}
		set
		{
			if (_modeInitialized != value)
			{
				_modeInitialized = value;
				OnPropertyChanged("ModeInitialized");
			}
		}
	}

	public string CustomType
	{
		get
		{
			return Keychain.CustomType;
		}
		set
		{
			if (!string.Equals(CustomType, value, StringComparison.Ordinal))
			{
				Keychain.CustomType = value;
				OnPropertyChanged("CustomType");
				OnPropertyChanged("KeychainColor");
			}
		}
	}

	public Visibility CustomVisibility
	{
		get
		{
			if ((int)Type != 4)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public bool CustomTypeInitialized
	{
		get
		{
			return _customTypeInitialized;
		}
		set
		{
			if (_customTypeInitialized != value)
			{
				_customTypeInitialized = value;
				OnPropertyChanged("CustomTypeInitialized");
			}
		}
	}

	public SolidColorBrush KeychainColor
	{
		get
		{
			Color color = Colors.Black;
			KeyChain.KeychainType type = Type;
			switch ((int)type)
			{
			case 0:
				color = (Color)ColorConverter.ConvertFromString("#006600");
				break;
			case 1:
				color = (Color)ColorConverter.ConvertFromString("#cc0000");
				break;
			case 4:
				color = SB.Settings.Environment.GetCustomKeychain(CustomType).Color;
				break;
			case 2:
				color = (Color)ColorConverter.ConvertFromString("#660066");
				break;
			case 3:
				color = (Color)ColorConverter.ConvertFromString("#cc9900");
				break;
			}
			return new SolidColorBrush(color);
		}
	}

	public KeyViewModel GetKeyById(int id)
	{
		return KeyList.Where((KeyViewModel x) => x.Id.KeyId == id).First();
	}

	public void RemoveKeyById(int id)
	{
		Keychain.Keys.Remove(GetKeyById(id).Key);
		KeyList.Remove(GetKeyById(id));
	}

	public void AddKey()
	{
		Key val = new Key();
		Keychain.Keys.Add(val);
		KeyList.Add(new KeyViewModel(val, rand.Next(), Id));
	}

	public KeychainViewModel(KeyChain keychain, int id)
	{
		Keychain = keychain;
		Id = id;
		TypeInitialized = false;
		ModeInitialized = false;
		CustomTypeInitialized = false;
		foreach (Key key in keychain.Keys)
		{
			KeyList.Add(new KeyViewModel(key, rand.Next(), Id));
		}
	}
}
