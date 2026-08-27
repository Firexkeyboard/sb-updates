using System;
using System.Linq;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

internal class ListGeneratorViewModel : ViewModelBase
{
	private bool onlyLuhn;

	private bool autoImport;

	private string mask = "657438923467423847****:**";

	private string allowedCharacters = "0123456789";

	private static readonly string[] SizeSuffixes = new string[9] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

	public bool OnlyLuhn
	{
		get
		{
			return onlyLuhn;
		}
		set
		{
			if (onlyLuhn != value)
			{
				onlyLuhn = value;
				OnPropertyChanged("OnlyLuhn");
				OnPropertyChanged("OutputLines");
				OnPropertyChanged("OutputSize");
			}
		}
	}

	public bool AutoImport
	{
		get
		{
			return autoImport;
		}
		set
		{
			if (autoImport != value)
			{
				autoImport = value;
				OnPropertyChanged("AutoImport");
			}
		}
	}

	public string Mask
	{
		get
		{
			return mask;
		}
		set
		{
			if (!string.Equals(mask, value, StringComparison.Ordinal))
			{
				mask = value;
				OnPropertyChanged("Mask");
				OnPropertyChanged("OutputLines");
				OnPropertyChanged("OutputSize");
			}
		}
	}

	public string AllowedCharacters
	{
		get
		{
			return allowedCharacters;
		}
		set
		{
			if (!string.Equals(allowedCharacters, value, StringComparison.Ordinal))
			{
				allowedCharacters = value;
				OnPropertyChanged("AllowedCharacters");
				OnPropertyChanged("OutputLines");
				OnPropertyChanged("OutputSize");
			}
		}
	}

	public int OutputLines
	{
		get
		{
			string text = Mask.Split(':')[0].Replace("*", "");
			int num = (int)Math.Pow(y: (from c in Mask.ToCharArray()
				where c == '*'
				select c).Count(), x: AllowedCharacters.Length);
			if (text.ToCharArray().Any((char c) => !char.IsDigit(c)) || AllowedCharacters.ToCharArray().Any((char c) => !char.IsDigit(c)) || !OnlyLuhn)
			{
				return num;
			}
			return num / 10;
		}
	}

	public string OutputSize => SizeSuffix(2 * Mask.Length * OutputLines, 0);

	private static string SizeSuffix(long value, int decimalPlaces = 1)
	{
		if (decimalPlaces < 0)
		{
			throw new ArgumentOutOfRangeException("decimalPlaces");
		}
		if (value < 0)
		{
			return "-" + SizeSuffix(-value);
		}
		if (value == 0L)
		{
			return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
		}
		int num = (int)Math.Log(value, 1024.0);
		decimal num2 = (decimal)value / (decimal)(1L << num * 10);
		if (Math.Round(num2, decimalPlaces) >= 1000m)
		{
			num++;
			num2 /= 1024m;
		}
		return string.Format("{0:n" + decimalPlaces + "} {1}", num2, SizeSuffixes[num]);
	}
}
