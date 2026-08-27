using System.Collections.Generic;
using System.Reflection;
using RuriLib.Enums;

namespace RuriLib.ViewModels;

public class SettingsCaptchas : ViewModelBase
{
	private CaptchaServiceType currentService;

	private string antiCapToken = "";

	private string imageTypToken = "";

	private string dbcUser = "";

	private string dbcPass = "";

	private string twoCapToken = "";

	private string ruCapToken = "";

	private string dcUser = "";

	private string dcPass = "";

	private string azCapToken = "";

	private string scToken = "";

	private string srUserId = "";

	private string srToken = "";

	private string trueCapUser = "";

	private string trueCapToken = "";

	private string cIOToken = "";

	private string cdToken = "";

	private string customTwoCapToken = "";

	private string customTwoCapDomain = "example.com";

	private int customTwoCapPort = 80;

	private bool bypassBalanceCheck;

	private int timeout = 120;

	public CaptchaServiceType CurrentService
	{
		get
		{
			return currentService;
		}
		set
		{
			currentService = value;
			OnPropertyChanged("CurrentService");
		}
	}

	public string AntiCapToken
	{
		get
		{
			return antiCapToken;
		}
		set
		{
			antiCapToken = value;
			OnPropertyChanged("AntiCapToken");
		}
	}

	public string ImageTypToken
	{
		get
		{
			return imageTypToken;
		}
		set
		{
			imageTypToken = value;
			OnPropertyChanged("ImageTypToken");
		}
	}

	public string DBCUser
	{
		get
		{
			return dbcUser;
		}
		set
		{
			dbcUser = value;
			OnPropertyChanged("DBCUser");
		}
	}

	public string DBCPass
	{
		get
		{
			return dbcPass;
		}
		set
		{
			dbcPass = value;
			OnPropertyChanged("DBCPass");
		}
	}

	public string TwoCapToken
	{
		get
		{
			return twoCapToken;
		}
		set
		{
			twoCapToken = value;
			OnPropertyChanged("TwoCapToken");
		}
	}

	public string RuCapToken
	{
		get
		{
			return ruCapToken;
		}
		set
		{
			ruCapToken = value;
			OnPropertyChanged("RuCapToken");
		}
	}

	public string DCUser
	{
		get
		{
			return dcUser;
		}
		set
		{
			dcUser = value;
			OnPropertyChanged("DCUser");
		}
	}

	public string DCPass
	{
		get
		{
			return dcPass;
		}
		set
		{
			dcPass = value;
			OnPropertyChanged("DCPass");
		}
	}

	public string AZCapToken
	{
		get
		{
			return azCapToken;
		}
		set
		{
			azCapToken = value;
			OnPropertyChanged("AZCapToken");
		}
	}

	public string SCToken
	{
		get
		{
			return scToken;
		}
		set
		{
			scToken = value;
			OnPropertyChanged("SCToken");
		}
	}

	public string SRUserId
	{
		get
		{
			return srUserId;
		}
		set
		{
			srUserId = value;
			OnPropertyChanged("SRUserId");
		}
	}

	public string SRToken
	{
		get
		{
			return srToken;
		}
		set
		{
			srToken = value;
			OnPropertyChanged("SRToken");
		}
	}

	public string TrueCapUser
	{
		get
		{
			return trueCapUser;
		}
		set
		{
			trueCapUser = value;
			OnPropertyChanged("TrueCapUser");
		}
	}

	public string TrueCapToken
	{
		get
		{
			return trueCapToken;
		}
		set
		{
			trueCapToken = value;
			OnPropertyChanged("TrueCapToken");
		}
	}

	public string CIOToken
	{
		get
		{
			return cIOToken;
		}
		set
		{
			cIOToken = value;
			OnPropertyChanged("CIOToken");
		}
	}

	public string CDToken
	{
		get
		{
			return cdToken;
		}
		set
		{
			cdToken = value;
			OnPropertyChanged("CDToken");
		}
	}

	public string CustomTwoCapToken
	{
		get
		{
			return customTwoCapToken;
		}
		set
		{
			customTwoCapToken = value;
			OnPropertyChanged("CustomTwoCapToken");
		}
	}

	public string CustomTwoCapDomain
	{
		get
		{
			return customTwoCapDomain;
		}
		set
		{
			customTwoCapDomain = value;
			OnPropertyChanged("CustomTwoCapDomain");
		}
	}

	public int CustomTwoCapPort
	{
		get
		{
			return customTwoCapPort;
		}
		set
		{
			customTwoCapPort = value;
			OnPropertyChanged("CustomTwoCapPort");
		}
	}

	public bool BypassBalanceCheck
	{
		get
		{
			return bypassBalanceCheck;
		}
		set
		{
			bypassBalanceCheck = value;
			OnPropertyChanged("BypassBalanceCheck");
		}
	}

	public int Timeout
	{
		get
		{
			return timeout;
		}
		set
		{
			timeout = value;
			OnPropertyChanged("Timeout");
		}
	}

	public void Reset()
	{
		SettingsCaptchas obj = new SettingsCaptchas();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(SettingsCaptchas).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
