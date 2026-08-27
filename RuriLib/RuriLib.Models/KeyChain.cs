using System.Collections.ObjectModel;
using System.Windows.Media;
using RuriLib.ViewModels;

namespace RuriLib.Models;

public class KeyChain : ViewModelBase
{
	public enum KeychainType
	{
		Success,
		Failure,
		Ban,
		Retry,
		Custom
	}

	public enum KeychainMode
	{
		OR,
		AND
	}

	private KeychainType type;

	private KeychainMode mode;

	private string customType = "CUSTOM";

	public KeychainType Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
			OnPropertyChanged("Type");
		}
	}

	public KeychainMode Mode
	{
		get
		{
			return mode;
		}
		set
		{
			mode = value;
			OnPropertyChanged("Mode");
		}
	}

	public string CustomType
	{
		get
		{
			return customType;
		}
		set
		{
			customType = value;
			OnPropertyChanged("CustomType");
		}
	}

	public ObservableCollection<Key> Keys { get; set; } = new ObservableCollection<Key>();

	public bool CheckKeys(BotData data)
	{
		switch (Mode)
		{
		case KeychainMode.OR:
			foreach (Key key in Keys)
			{
				if (key.CheckKey(data))
				{
					data.Log(new LogEntry($"Found 'OR' Key {BlockBase.TruncatePretty(BlockBase.ReplaceValues(key.LeftTerm, data), 20)} {key.Comparer.ToString()} {BlockBase.ReplaceValues(key.RightTerm, data)}", Colors.White));
					return true;
				}
			}
			return false;
		case KeychainMode.AND:
			foreach (Key key2 in Keys)
			{
				if (!key2.CheckKey(data))
				{
					return false;
				}
				data.Log(new LogEntry($"Found 'AND' Key {BlockBase.TruncatePretty(BlockBase.ReplaceValues(key2.LeftTerm, data), 20)} {key2.Comparer.ToString()} {BlockBase.ReplaceValues(key2.RightTerm, data)}", Colors.White));
			}
			return true;
		default:
			return false;
		}
	}
}
