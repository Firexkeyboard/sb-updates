using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenBullet.Converters;

public class BooleanBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return new SolidColorBrush((value?.Equals(true)).Value ? Colors.Tomato : Colors.Yellow);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return false;
	}
}
