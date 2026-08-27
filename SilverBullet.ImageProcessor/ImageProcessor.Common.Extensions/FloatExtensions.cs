using System;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Common.Extensions;

public static class FloatExtensions
{
	public static byte ToByte(this float value)
	{
		return Convert.ToByte(ImageMaths.Clamp(value, 0f, 255f));
	}
}
