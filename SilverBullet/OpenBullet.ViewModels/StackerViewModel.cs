using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using OpenBullet.Views.StackerBlocks;
using RuriLib.Models;
using RuriLib;
using RuriLib.LS;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class StackerViewModel : ViewModelBase
{
	private Random rand = new Random();

	private StackerView _view;

	private BotData _botData;

	private LoliScript _lS;

	private bool controlsEnabled = true;

	private ConfigViewModel _config;

	private ObservableCollection<StackerBlockViewModel> _stack = new ObservableCollection<StackerBlockViewModel>();

	private StackerBlockViewModel _currentBlock;

	private BlockBase _lastDeletedBlock;

	private int _lastDeletedIndex;

	private ProxyType proxyType;

	private string testData = "";

	private string testDataType = "";

	private string testProxy = "";

	private bool useProxy;

	private bool sbs;

	private bool sbsClear;

	private bool sbsEnabled;

	private string searchString = "";

	private int totalSearchMatches;

	private bool disableToolTip;

	private bool scriptCompletion;

	private int currentSearchMatch;

	public StackerView View
	{
		get
		{
			return _view;
		}
		set
		{
			if (_view != value)
			{
				_view = value;
				OnPropertyChanged("View");
			}
		}
	}

	public BotData BotData
	{
		get
		{
			return _botData;
		}
		set
		{
			if (!object.Equals(_botData, value))
			{
				_botData = value;
				OnPropertyChanged("BotData");
			}
		}
	}

	public LoliScript LS
	{
		get
		{
			return _lS;
		}
		set
		{
			if (!object.Equals(_lS, value))
			{
				_lS = value;
				OnPropertyChanged("LS");
			}
		}
	}

	public bool ControlsEnabled
	{
		get
		{
			return controlsEnabled;
		}
		set
		{
			if (controlsEnabled != value)
			{
				controlsEnabled = value;
				OnPropertyChanged("ControlsEnabled");
			}
		}
	}

	public ConfigViewModel Config
	{
		get
		{
			return _config;
		}
		set
		{
			if (!object.Equals(_config, value))
			{
				_config = value;
				OnPropertyChanged("Config");
			}
		}
	}

	public ObservableCollection<StackerBlockViewModel> Stack
	{
		get
		{
			return _stack;
		}
		set
		{
			if (!object.Equals(_stack, value))
			{
				_stack = value;
				OnPropertyChanged("SelectedBlocks");
				OnPropertyChanged("CurrentBlockIndex");
				OnPropertyChanged("Stack");
			}
		}
	}

	public List<StackerBlockViewModel> SelectedBlocks => Stack.Where((StackerBlockViewModel b) => b.Selected).ToList();

	public StackerBlockViewModel CurrentBlock
	{
		get
		{
			return _currentBlock;
		}
		set
		{
			if (!object.Equals(_currentBlock, value))
			{
				_currentBlock = value;
				OnPropertyChanged("CurrentBlockIndex");
				OnPropertyChanged("CurrentBlock");
			}
		}
	}

	public int CurrentBlockIndex => Stack.IndexOf(CurrentBlock);

	public BlockBase LastDeletedBlock
	{
		get
		{
			return _lastDeletedBlock;
		}
		set
		{
			if (!object.Equals(_lastDeletedBlock, value))
			{
				_lastDeletedBlock = value;
				OnPropertyChanged("LastDeletedBlock");
			}
		}
	}

	public int LastDeletedIndex
	{
		get
		{
			return _lastDeletedIndex;
		}
		set
		{
			if (_lastDeletedIndex != value)
			{
				_lastDeletedIndex = value;
				OnPropertyChanged("LastDeletedIndex");
			}
		}
	}

	public ProxyType ProxyType
	{
		get
		{
			return proxyType;
		}
		set
		{
			if (proxyType != value)
			{
				proxyType = value;
				OnPropertyChanged("ProxyType");
			}
		}
	}

	public string TestData
	{
		get
		{
			return testData;
		}
		set
		{
			if (!string.Equals(testData, value, StringComparison.Ordinal))
			{
				testData = value;
				OnPropertyChanged("TestData");
			}
		}
	}

	public string TestDataType
	{
		get
		{
			return testDataType;
		}
		set
		{
			if (!string.Equals(testDataType, value, StringComparison.Ordinal))
			{
				testDataType = value;
				OnPropertyChanged("TestDataType");
			}
		}
	}

	public string TestProxy
	{
		get
		{
			return testProxy;
		}
		set
		{
			if (!string.Equals(testProxy, value, StringComparison.Ordinal))
			{
				testProxy = value;
				OnPropertyChanged("TestProxy");
			}
		}
	}

	public bool UseProxy
	{
		get
		{
			return useProxy;
		}
		set
		{
			if (useProxy != value)
			{
				useProxy = value;
				OnPropertyChanged("UseProxy");
				OnPropertyChanged("UseProxyString");
				OnPropertyChanged("UseProxyColor");
			}
		}
	}

	public string UseProxyString
	{
		get
		{
			if (!UseProxy)
			{
				return "OFF";
			}
			return "ON";
		}
	}

	public SolidColorBrush UseProxyColor
	{
		get
		{
			if (!UseProxy)
			{
				return Utils.GetBrush("ForegroundBad");
			}
			return Utils.GetBrush("ForegroundGood");
		}
	}

	public bool SBS
	{
		get
		{
			return sbs;
		}
		set
		{
			if (sbs != value)
			{
				sbs = value;
				OnPropertyChanged("SBS");
			}
		}
	}

	public bool SBSClear
	{
		get
		{
			return sbsClear;
		}
		set
		{
			if (sbsClear != value)
			{
				sbsClear = value;
				OnPropertyChanged("SBSClear");
			}
		}
	}

	public bool SBSEnabled
	{
		get
		{
			return sbsEnabled;
		}
		set
		{
			if (sbsEnabled != value)
			{
				sbsEnabled = value;
				OnPropertyChanged("SBSEnabled");
			}
		}
	}

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

	public int TotalSearchMatches
	{
		get
		{
			return totalSearchMatches;
		}
		set
		{
			if (totalSearchMatches != value)
			{
				totalSearchMatches = value;
				OnPropertyChanged("TotalSearchMatches");
			}
		}
	}

	public bool DisableToolTip
	{
		get
		{
			return disableToolTip;
		}
		set
		{
			if (disableToolTip != value)
			{
				disableToolTip = value;
				OnPropertyChanged("DisableToolTip");
			}
		}
	}

	public bool ScriptCompletion
	{
		get
		{
			return scriptCompletion;
		}
		set
		{
			if (scriptCompletion != value)
			{
				scriptCompletion = value;
				OnPropertyChanged("ScriptCompletion");
			}
		}
	}

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

	public void DeselectAll()
	{
		foreach (StackerBlockViewModel item in Stack)
		{
			item.Selected = false;
		}
	}

	public void SelectAll()
	{
		foreach (StackerBlockViewModel item in Stack)
		{
			item.Selected = true;
		}
	}

	public List<BlockBase> GetList()
	{
		ConvertKeychains();
		ConvertPlugins();
		return Stack.Select((StackerBlockViewModel x) => x.Block).ToList();
	}

	public void ConvertKeychains()
	{
		foreach (StackerBlockViewModel item in Stack)
		{
			BlockBase block = item.Block;
			Page page = item.Page;
			if (page.GetType() == typeof(PageBlockKeycheck))
			{
				PageBlockKeycheck pageBlockKeycheck = (PageBlockKeycheck)page;
				((BlockKeycheck)block).KeyChains = pageBlockKeycheck.vm.KeychainList.Select((KeychainViewModel k) => k.Keychain).ToList();
			}
		}
	}

	public void ConvertPlugins()
	{
		foreach (StackerBlockViewModel item in Stack.Where((StackerBlockViewModel sb) => sb.Block.IsPlugin()))
		{
			(item.Page as BlockPluginPage).SetPropertyValues();
		}
	}

	public StackerBlockViewModel GetBlockById(int id)
	{
		return Stack.Where((StackerBlockViewModel x) => x.Id == id).First();
	}

	public void AddBlock(BlockBase block, int index = -1)
	{
		if (index < 0 || index > Stack.Count)
		{
			index = Stack.Count;
		}
		Stack.Insert(index, new StackerBlockViewModel(block, rand));
		UpdateHeights();
	}

	public void ClearBlocks()
	{
		Stack.Clear();
		UpdateHeights();
	}

	public void UpdateHeights()
	{
		foreach (StackerBlockViewModel item in Stack)
		{
			item.UpdateHeight(Clamp(600 / Stack.Count, 30, 60));
		}
	}

	public void MoveBlockUp(StackerBlockViewModel block)
	{
		int num = Stack.IndexOf(block);
		if (num != 0)
		{
			Stack.Move(num, num - 1);
		}
	}

	public void MoveBlockDown(StackerBlockViewModel block)
	{
		int num = Stack.IndexOf(block);
		if (num != Stack.Count - 1)
		{
			Stack.Move(num, num + 1);
		}
	}

	public int Clamp(int a, int min, int max)
	{
		if (a < min)
		{
			return min;
		}
		if (a > max)
		{
			return max;
		}
		return a;
	}

	public StackerViewModel(ConfigViewModel config)
	{
		Config = config;
	}
}
