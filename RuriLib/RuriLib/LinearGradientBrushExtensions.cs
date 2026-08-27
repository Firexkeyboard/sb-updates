using System.Windows.Media;

namespace RuriLib;

public static class LinearGradientBrushExtensions
{
	public static LinearGradientBrush GetLinearGradientBrush(this Color color)
	{
		return new LinearGradientBrush(new GradientStopCollection
		{
			new GradientStop
			{
				Color = color
			}
		});
	}

	public static Color ColorConverter(this string color)
	{
		return (Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
	}
}
