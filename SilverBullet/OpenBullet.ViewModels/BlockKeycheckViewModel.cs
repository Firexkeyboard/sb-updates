using System;
using System.Collections.ObjectModel;
using System.Linq;
using RuriLib;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class BlockKeycheckViewModel : ViewModelBase
{
	private Random rand = new Random(2);

	private BlockKeycheck _block;

	private ObservableCollection<KeychainViewModel> _keychainList;

	public BlockKeycheck Block
	{
		get
		{
			return _block;
		}
		set
		{
			if (!object.Equals(_block, value))
			{
				_block = value;
				OnPropertyChanged("Block");
			}
		}
	}

	public ObservableCollection<KeychainViewModel> KeychainList
	{
		get
		{
			return _keychainList;
		}
		set
		{
			if (!object.Equals(_keychainList, value))
			{
				_keychainList = value;
				OnPropertyChanged("KeychainList");
			}
		}
	}

	public KeychainViewModel GetKeychainById(int id)
	{
		return KeychainList.Where((KeychainViewModel x) => x.Id == id).First();
	}

	public void RemoveKeychainById(int id)
	{
		KeychainList.Remove(GetKeychainById(id));
	}

	public void AddKeychain()
	{
		KeychainList.Add(new KeychainViewModel(new KeyChain(), rand.Next()));
	}

	public BlockKeycheckViewModel(BlockKeycheck block)
	{
		Block = block;
		KeychainList = new ObservableCollection<KeychainViewModel>();
		foreach (KeyChain keyChain in block.KeyChains)
		{
			KeychainList.Add(new KeychainViewModel(keyChain, rand.Next()));
		}
	}
}
