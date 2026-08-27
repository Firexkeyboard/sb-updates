using System;
using System.Drawing;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Colors;

internal static class ColorExtensions
{
	public static Color Add(this Color color, params Color[] colors)
	{
		int num = ((color.A > 0) ? color.R : 0);
		int num2 = ((color.A > 0) ? color.G : 0);
		int num3 = ((color.A > 0) ? color.B : 0);
		int num4 = color.A;
		int num5 = 0;
		for (int i = 0; i < colors.Length; i++)
		{
			Color color2 = colors[i];
			if (color2.A > 0)
			{
				num5++;
				num += color2.R;
				num2 += color2.G;
				num3 += color2.B;
				num4 += color2.A;
			}
		}
		num5 = Math.Max(1, num5);
		return Color.FromArgb((num4 / num5).ToByte(), (num / num5).ToByte(), (num2 / num5).ToByte(), (num3 / num5).ToByte());
	}

	public static CmykColor AddAsCmykColor(this Color color, params Color[] colors)
	{
		CmykColor cmykColor = color;
		float num = ((color.A > 0) ? cmykColor.C : 0f);
		float num2 = ((color.A > 0) ? cmykColor.M : 0f);
		float num3 = ((color.A > 0) ? cmykColor.Y : 0f);
		float num4 = ((color.A > 0) ? cmykColor.K : 0f);
		for (int i = 0; i < colors.Length; i++)
		{
			Color color2 = colors[i];
			if (color2.A > 0)
			{
				CmykColor cmykColor2 = color2;
				num += cmykColor2.C;
				num2 += cmykColor2.M;
				num3 += cmykColor2.Y;
				num4 += cmykColor2.K;
			}
		}
		return CmykColor.FromCmykColor(num, num2, num3, num4);
	}
}
