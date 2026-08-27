using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using OpenBullet.Models;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class OBSettingsSources : ViewModelBase
{
	private ObservableCollection<Source> _sources = new ObservableCollection<Source>();

	public ObservableCollection<Source> Sources
	{
		get
		{
			return _sources;
		}
		set
		{
			if (!object.Equals(_sources, value))
			{
				_sources = value;
				OnPropertyChanged("Sources");
			}
		}
	}

	public void RemoveSourceById(int id)
	{
		Sources.Remove(GetSourceById(id));
	}

	public Source GetSourceById(int id)
	{
		return Sources.FirstOrDefault((Source s) => s.Id == id);
	}

	public void Reset()
	{
		OBSettingsSources obj = new OBSettingsSources();
		foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)new List<PropertyInfo>(typeof(OBSettingsSources).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
