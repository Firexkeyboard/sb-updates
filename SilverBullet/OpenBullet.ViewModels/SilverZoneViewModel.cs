using System;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class SilverZoneViewModel : ViewModelBase
{
	private string supportersBadge;

	private string verifiedMarketBadge;

	public string SupportersBadge
	{
		get
		{
			return supportersBadge;
		}
		set
		{
			if (!string.Equals(supportersBadge, value, StringComparison.Ordinal))
			{
				supportersBadge = value;
				OnPropertyChanged("SupportersBadge");
			}
		}
	}

	public string VerifiedMarketBadge
	{
		get
		{
			return verifiedMarketBadge;
		}
		set
		{
			if (!string.Equals(verifiedMarketBadge, value, StringComparison.Ordinal))
			{
				verifiedMarketBadge = value;
				OnPropertyChanged("VerifiedMarketBadge");
			}
		}
	}
}
