using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace RuriLib.ViewModels;

public class SettingsOcr : ViewModelBase
{
	private bool saveImageToCaptchasFolder;

	private bool getIterator;

	private ObservableCollection<TesseractVariable> variableList = new ObservableCollection<TesseractVariable>();

	public bool SaveImageToCaptchasFolder
	{
		get
		{
			return saveImageToCaptchasFolder;
		}
		set
		{
			saveImageToCaptchasFolder = value;
			OnPropertyChanged("SaveImageToCaptchasFolder");
		}
	}

	public bool GetIterator
	{
		get
		{
			return getIterator;
		}
		set
		{
			getIterator = value;
			OnPropertyChanged("GetIterator");
		}
	}

	public ObservableCollection<TesseractVariable> VariableList
	{
		get
		{
			return variableList;
		}
		set
		{
			variableList = value;
			OnPropertyChanged("VariableList");
		}
	}

	public void Reset()
	{
		SettingsOcr obj = new SettingsOcr();
		foreach (PropertyInfo item in new List<PropertyInfo>(typeof(SettingsOcr).GetProperties()))
		{
			item.SetValue(this, item.GetValue(obj, null));
		}
	}
}
