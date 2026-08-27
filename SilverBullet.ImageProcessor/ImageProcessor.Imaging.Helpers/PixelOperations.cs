using System;
using System.Drawing;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Helpers;

public static class PixelOperations
{
	private static readonly Lazy<byte[]> LinearBytes = new Lazy<byte[]>(GetLinearBytes);

	private static readonly Lazy<byte[]> SRGBBytes = new Lazy<byte[]>(GetSRGBBytes);

	private static readonly Lazy<byte[]> LinearGammaBytes = new Lazy<byte[]>(GetLinearGammaBytes);

	private static readonly Lazy<byte[]> GammaLinearBytes = new Lazy<byte[]>(GetGammaLinearBytes);

	public static Color Gamma(Color color, float value)
	{
		if (value > 5f || (double)value < 0.1)
		{
			throw new ArgumentOutOfRangeException("value", "Value should be between .1 and 5.");
		}
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255.0 * Math.Pow((double)i / 255.0, value) + 0.5).ToByte();
		}
		byte red = array[color.R];
		byte green = array[color.G];
		byte blue = array[color.B];
		return Color.FromArgb(color.A, red, green, blue);
	}

	public static Color ToLinear(Color composite)
	{
		byte[] value = LinearBytes.Value;
		return Color.FromArgb(composite.A, value[composite.R], value[composite.G], value[composite.B]);
	}

	public static Color ToSRGB(Color linear)
	{
		byte[] value = SRGBBytes.Value;
		return Color.FromArgb(linear.A, value[linear.R], value[linear.G], value[linear.B]);
	}

	public static Color ToLinearFromGamma(Color composite)
	{
		byte[] value = LinearGammaBytes.Value;
		return Color.FromArgb(composite.A, value[composite.R], value[composite.G], value[composite.B]);
	}

	public static Color ToGammaFromLinear(Color composite)
	{
		byte[] value = GammaLinearBytes.Value;
		return Color.FromArgb(composite.A, value[composite.R], value[composite.G], value[composite.B]);
	}

	private static byte[] GetLinearGammaBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255.0 * Math.Pow((float)i / 255f, 2.2)).ToByte();
		}
		return array;
	}

	private static byte[] GetGammaLinearBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255.0 * Math.Pow((float)i / 255f, 0.45454545454545453)).ToByte();
		}
		return array;
	}

	private static byte[] GetLinearBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255f * SRGBToLinear((float)i / 255f)).ToByte();
		}
		return array;
	}

	private static byte[] GetSRGBBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255f * LinearToSRGB((float)i / 255f)).ToByte();
		}
		return array;
	}

	private static float SRGBToLinear(float signal)
	{
		if ((double)signal <= 0.04045)
		{
			return signal / 12.92f;
		}
		return (float)Math.Pow((signal + 0.055f) / 1.055f, 2.4);
	}

	private static float LinearToSRGB(float signal)
	{
		if ((double)signal <= 0.0031308)
		{
			return signal * 12.92f;
		}
		return (float)(1.0549999475479126 * Math.Pow(signal, 0.4166666567325592)) - 0.055f;
	}
}
