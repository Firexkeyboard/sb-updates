using System.Windows.Media;

namespace OpenBullet;

public static class ColorExtensions
{
	public static uint ColorToUInt(this Color color)
	{
		return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
	}

	public static string ConvertToString(this Color c)
	{
		return c.R + "," + c.G + "," + c.B;
	}
}
