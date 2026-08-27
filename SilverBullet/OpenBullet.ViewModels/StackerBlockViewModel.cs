using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class StackerBlockViewModel : ViewModelBase
{
	private int id;

	private bool selected;

	private int height;

	private Page page;

	private BlockBase block;

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

	public bool Selected
	{
		get
		{
			return selected;
		}
		set
		{
			if (selected != value)
			{
				selected = value;
				OnPropertyChanged("Selected");
				OnPropertyChanged("BorderColor");
			}
		}
	}

	public SolidColorBrush BorderColor => new SolidColorBrush(Selected ? Colors.White : Colors.Black);

	public LinearGradientBrush Color
	{
		get
		{
			if (!Block.Disabled)
			{
				return GetBlockColor();
			}
			return LinearGradientBrushExtensions.GetLinearGradientBrush(Colors.Gray);
		}
	}

	public SolidColorBrush Foreground => new SolidColorBrush(Block.IsSelenium ? Colors.White : Colors.Black);

	public int Height
	{
		get
		{
			return height;
		}
		set
		{
			if (height != value)
			{
				height = value;
				OnPropertyChanged("Height");
				OnPropertyChanged("FontSize");
			}
		}
	}

	public int FontSize => Height / 3;

	public Page Page
	{
		get
		{
			return page;
		}
		set
		{
			if (!object.Equals(page, value))
			{
				page = value;
				OnPropertyChanged("Page");
			}
		}
	}

	public BlockBase Block
	{
		get
		{
			return block;
		}
		set
		{
			if (!object.Equals(block, value))
			{
				block = value;
				OnPropertyChanged("Color");
				OnPropertyChanged("Foreground");
				OnPropertyChanged("Block");
			}
		}
	}

	public void Disable()
	{
		if (!(((object)block).GetType() == typeof(BlockLSCode)))
		{
			Block.Disabled = !Block.Disabled;
			OnPropertyChanged("Color");
		}
	}

	public void UpdateHeight(int height)
	{
		Height = height;
		OnPropertyChanged("Height");
		OnPropertyChanged("FontSize");
	}

	public StackerBlockViewModel(BlockBase block, Random rand)
	{
		Id = rand.Next();
		Block = block;
		OnPropertyChanged("Label");
		OnPropertyChanged("Color");
		if (SB.BlockMappings.Any(((Type, Type, LinearGradientBrush) m) => m.Item1 == ((object)block).GetType()))
		{
			Page = Activator.CreateInstance(SB.BlockMappings.First(((Type, Type, LinearGradientBrush) m) => m.Item1 == ((object)block).GetType()).Item2, block) as Page;
			return;
		}
		throw new Exception("Tried to initialize a page for the block type " + ((object)block).GetType().Name + " but it wasn't found in the mappings");
	}

	public LinearGradientBrush GetBlockColor()
	{
		return SB.BlockMappings.First(((Type, Type, LinearGradientBrush) m) => m.Item1 == ((object)block).GetType()).Item3;
	}
}
