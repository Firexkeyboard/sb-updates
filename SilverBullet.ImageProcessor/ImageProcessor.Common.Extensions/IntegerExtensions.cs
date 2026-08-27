using System;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Common.Extensions;

public static class IntegerExtensions
{
	public static byte ToByte(this int value)
	{
		return Convert.ToByte(ImageMaths.Clamp(value, 0, 255));
	}
}
