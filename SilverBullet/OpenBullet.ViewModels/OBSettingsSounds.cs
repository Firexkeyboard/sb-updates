using System;
using System.Collections.Generic;
using System.Reflection;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class OBSettingsSounds : ViewModelBase
{
	private bool enableSounds;

	private string onHitSound = "rifle_hit.wav";

	private string onReloadSound = "rifle_reload.wav";

	public bool EnableSounds
	{
		get
		{
			return enableSounds;
		}
		set
		{
			if (enableSounds != value)
			{
				enableSounds = value;
				OnPropertyChanged("EnableSounds");
			}
		}
	}

	public string OnHitSound
	{
		get
		{
			return onHitSound;
		}
		set
		{
			if (!string.Equals(onHitSound, value, StringComparison.Ordinal))
			{
				onHitSound = value;
				OnPropertyChanged("OnHitSound");
			}
		}
	}

	public string OnReloadSound
	{
		get
		{
			return onReloadSound;
		}
		set
		{
			if (!string.Equals(onReloadSound, value, StringComparison.Ordinal))
			{
				onReloadSound = value;
				OnPropertyChanged("OnReloadSound");
			}
		}
	}

	public void Reset()
	{
		OBSettingsSounds obj = new OBSettingsSounds();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(OBSettingsSounds).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
