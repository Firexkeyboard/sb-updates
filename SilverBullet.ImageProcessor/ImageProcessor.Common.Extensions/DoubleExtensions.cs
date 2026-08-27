using System;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Common.Extensions;

public static class DoubleExtensions
{
	public static byte ToByte(this double value)
	{
		return Convert.ToByte(ImageMaths.Clamp(value, 0.0, 255.0));
	}
}
