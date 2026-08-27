using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenBullet.Converters;

public class EnumBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value?.Equals(parameter);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || !value.Equals(true))
		{
			return Binding.DoNothing;
		}
		return parameter;
	}
}
