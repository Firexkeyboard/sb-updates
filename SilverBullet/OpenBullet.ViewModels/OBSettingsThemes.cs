using System;
using System.Collections.Generic;
using System.Reflection;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class OBSettingsThemes : ViewModelBase
{
	private string backgroundMain = "#222";

	private string backgroundSecondary = "#111";

	private string foregroundMain = "#dcdcdc";

	private string foregroundGood = "#adff2f";

	private string foregroundBad = "#ff6347";

	private string foregroundFree = "#ff8c00";

	private string foregroundRetry = "#ffff00";

	private string foregroundToCheck = "#7fffd4";

	private string foregroundOcrRate = "#ff77bafd";

	private string foregroundMenuSelected = "#1e90ff";

	private bool useImage;

	private string backgroundImage = "";

	private int backgroundImageOpacity = 100;

	private string backgroundLogo = "";

	private bool enableSnow;

	private int snowAmount = 100;

	private bool allowTransparency;

	public string BackgroundMain
	{
		get
		{
			return backgroundMain;
		}
		set
		{
			if (!string.Equals(backgroundMain, value, StringComparison.Ordinal))
			{
				backgroundMain = value;
				OnPropertyChanged("BackgroundMain");
			}
		}
	}

	public string BackgroundSecondary
	{
		get
		{
			return backgroundSecondary;
		}
		set
		{
			if (!string.Equals(backgroundSecondary, value, StringComparison.Ordinal))
			{
				backgroundSecondary = value;
				OnPropertyChanged("BackgroundSecondary");
			}
		}
	}

	public string ForegroundMain
	{
		get
		{
			return foregroundMain;
		}
		set
		{
			if (!string.Equals(foregroundMain, value, StringComparison.Ordinal))
			{
				foregroundMain = value;
				OnPropertyChanged("ForegroundMain");
			}
		}
	}

	public string ForegroundGood
	{
		get
		{
			return foregroundGood;
		}
		set
		{
			if (!string.Equals(foregroundGood, value, StringComparison.Ordinal))
			{
				foregroundGood = value;
				OnPropertyChanged("ForegroundGood");
			}
		}
	}

	public string ForegroundBad
	{
		get
		{
			return foregroundBad;
		}
		set
		{
			if (!string.Equals(foregroundBad, value, StringComparison.Ordinal))
			{
				foregroundBad = value;
				OnPropertyChanged("ForegroundBad");
			}
		}
	}

	public string ForegroundCustom
	{
		get
		{
			return foregroundFree;
		}
		set
		{
			if (!string.Equals(foregroundFree, value, StringComparison.Ordinal))
			{
				foregroundFree = value;
				OnPropertyChanged("ForegroundCustom");
			}
		}
	}

	public string ForegroundRetry
	{
		get
		{
			return foregroundRetry;
		}
		set
		{
			if (!string.Equals(foregroundRetry, value, StringComparison.Ordinal))
			{
				foregroundRetry = value;
				OnPropertyChanged("ForegroundRetry");
			}
		}
	}

	public string ForegroundToCheck
	{
		get
		{
			return foregroundToCheck;
		}
		set
		{
			if (!string.Equals(foregroundToCheck, value, StringComparison.Ordinal))
			{
				foregroundToCheck = value;
				OnPropertyChanged("ForegroundToCheck");
			}
		}
	}

	public string ForegroundOcrRate
	{
		get
		{
			return foregroundOcrRate;
		}
		set
		{
			if (!string.Equals(foregroundOcrRate, value, StringComparison.Ordinal))
			{
				foregroundOcrRate = value;
				OnPropertyChanged("ForegroundOcrRate");
			}
		}
	}

	public string ForegroundMenuSelected
	{
		get
		{
			return foregroundMenuSelected;
		}
		set
		{
			if (!string.Equals(foregroundMenuSelected, value, StringComparison.Ordinal))
			{
				foregroundMenuSelected = value;
				OnPropertyChanged("ForegroundMenuSelected");
			}
		}
	}

	public bool UseImage
	{
		get
		{
			return useImage;
		}
		set
		{
			if (useImage != value)
			{
				useImage = value;
				OnPropertyChanged("UseImage");
			}
		}
	}

	public string BackgroundImage
	{
		get
		{
			return backgroundImage;
		}
		set
		{
			if (!string.Equals(backgroundImage, value, StringComparison.Ordinal))
			{
				backgroundImage = value;
				OnPropertyChanged("BackgroundImage");
			}
		}
	}

	public int BackgroundImageOpacity
	{
		get
		{
			return backgroundImageOpacity;
		}
		set
		{
			if (backgroundImageOpacity != value)
			{
				backgroundImageOpacity = value;
				OnPropertyChanged("BackgroundImageOpacity");
			}
		}
	}

	public string BackgroundLogo
	{
		get
		{
			return backgroundLogo;
		}
		set
		{
			if (!string.Equals(backgroundLogo, value, StringComparison.Ordinal))
			{
				backgroundLogo = value;
				OnPropertyChanged("BackgroundLogo");
			}
		}
	}

	public bool EnableSnow
	{
		get
		{
			return enableSnow;
		}
		set
		{
			if (enableSnow != value)
			{
				enableSnow = value;
				OnPropertyChanged("EnableSnow");
			}
		}
	}

	public int SnowAmount
	{
		get
		{
			return snowAmount;
		}
		set
		{
			if (snowAmount != value)
			{
				snowAmount = value;
				OnPropertyChanged("SnowAmount");
			}
		}
	}

	public bool AllowTransparency
	{
		get
		{
			return allowTransparency;
		}
		set
		{
			if (allowTransparency != value)
			{
				allowTransparency = value;
				OnPropertyChanged("AllowTransparency");
			}
		}
	}

	public void Reset()
	{
		OBSettingsThemes obj = new OBSettingsThemes();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(OBSettingsThemes).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
