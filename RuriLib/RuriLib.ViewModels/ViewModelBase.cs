using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuriLib.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
	private Dictionary<string, object> _properties = new Dictionary<string, object>();

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected T Get<T>([CallerMemberName] string name = null)
	{
		object value = null;
		if (_properties.TryGetValue(name, out value))
		{
			if (value != null)
			{
				return (T)value;
			}
			return default(T);
		}
		return default(T);
	}

	protected void Set<T>(T value, [CallerMemberName] string name = null)
	{
		if (!object.Equals(value, Get<T>(name)))
		{
			_properties[name] = value;
			OnPropertyChanged(name);
		}
	}

	protected void Set<T>(ref T field, T value)
	{
		MethodBase method = new StackFrame(1).GetMethod();
		field = value;
		OnPropertyChanged(method.Name.Substring(4));
	}
}
