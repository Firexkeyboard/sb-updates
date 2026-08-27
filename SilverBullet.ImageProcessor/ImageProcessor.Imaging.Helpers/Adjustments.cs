using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Helpers;

public static class Adjustments
{
	private static readonly Lazy<byte[]> LinearBytes = new Lazy<byte[]>(GetLinearBytes);

	private static readonly Lazy<byte[]> SRGBBytes = new Lazy<byte[]>(GetSRGBBytes);

	public static Bitmap Alpha(Image source, int percentage, Rectangle? rectangle = null)
	{
		if (percentage > 100 || percentage < 0)
		{
			throw new ArgumentOutOfRangeException("percentage", "Percentage should be between 0 and 100.");
		}
		float factor = (float)percentage / 100f;
		Rectangle bounds = rectangle ?? new Rectangle(0, 0, source.Width, source.Height);
		FastBitmap bitmap = new FastBitmap(source);
		try
		{
			Parallel.For(bounds.Y, bounds.Bottom, delegate(int y)
			{
				for (int i = bounds.X; i < bounds.Right; i++)
				{
					Color pixel = bitmap.GetPixel(i, y);
					bitmap.SetPixel(i, y, Color.FromArgb(Convert.ToInt32((float)(int)pixel.A * factor), pixel.R, pixel.G, pixel.B));
				}
			});
		}
		finally
		{
			if (bitmap != null)
			{
				((IDisposable)bitmap).Dispose();
			}
		}
		return (Bitmap)source;
	}

	public static Bitmap Brightness(Image source, int threshold, Rectangle? rectangle = null)
	{
		if (threshold > 100 || threshold < -100)
		{
			throw new ArgumentOutOfRangeException("threshold", "Threshold should be between -100 and 100.");
		}
		float num = (float)threshold / 100f;
		Rectangle destRect = rectangle ?? new Rectangle(0, 0, source.Width, source.Height);
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5] { 1f, 0f, 0f, 0f, 0f },
			new float[5] { 0f, 1f, 0f, 0f, 0f },
			new float[5] { 0f, 0f, 1f, 0f, 0f },
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5] { num, num, num, 0f, 1f }
		});
		using (Graphics graphics = Graphics.FromImage(source))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(colorMatrix);
			graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		return (Bitmap)source;
	}

	public static Bitmap Contrast(Image source, int threshold, Rectangle? rectangle = null)
	{
		if (threshold > 100 || threshold < -100)
		{
			throw new ArgumentOutOfRangeException("threshold", "Threshold should be between -100 and 100.");
		}
		Rectangle destRect = rectangle ?? new Rectangle(0, 0, source.Width, source.Height);
		float num = (float)threshold / 100f;
		num += 1f;
		float num2 = 0.5f * (1f - num);
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5] { num, 0f, 0f, 0f, 0f },
			new float[5] { 0f, num, 0f, 0f, 0f },
			new float[5] { 0f, 0f, num, 0f, 0f },
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5] { num2, num2, num2, 0f, 1f }
		});
		using (Graphics graphics = Graphics.FromImage(source))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(colorMatrix);
			graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		return (Bitmap)source;
	}

	public unsafe static Bitmap ContrastEx(Image source, sbyte threshold)
	{
		if (threshold > 100 || threshold < -100)
		{
			throw new ArgumentOutOfRangeException("threshold", "Threshold should be between -100 and 100.");
		}
		double num = 0.0;
		double num2 = (100.0 + (double)threshold) / 100.0;
		num2 *= num2;
		Bitmap bitmap = (Bitmap)source;
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
		int stride = bitmapData.Stride;
		byte* ptr = (byte*)(void*)bitmapData.Scan0;
		int num3 = stride - bitmap.Width * 3;
		for (int i = 0; i < bitmap.Height; i++)
		{
			for (int j = 0; j < bitmap.Width; j++)
			{
				byte num4 = *ptr;
				byte num5 = ptr[1];
				num = (double)(int)ptr[2] / 255.0;
				num -= 0.5;
				num *= num2;
				num += 0.5;
				num *= 255.0;
				if (num < 0.0)
				{
					num = 0.0;
				}
				if (num > 255.0)
				{
					num = 255.0;
				}
				ptr[2] = (byte)num;
				num = (double)(int)num5 / 255.0;
				num -= 0.5;
				num *= num2;
				num += 0.5;
				num *= 255.0;
				if (num < 0.0)
				{
					num = 0.0;
				}
				if (num > 255.0)
				{
					num = 255.0;
				}
				ptr[1] = (byte)num;
				num = (double)(int)num4 / 255.0;
				num -= 0.5;
				num *= num2;
				num += 0.5;
				num *= 255.0;
				if (num < 0.0)
				{
					num = 0.0;
				}
				if (num > 255.0)
				{
					num = 255.0;
				}
				*ptr = (byte)num;
				ptr += 3;
			}
			ptr += num3;
		}
		bitmap.UnlockBits(bitmapData);
		return bitmap;
	}

	public static Bitmap Gamma(Image source, float value)
	{
		if (value > 5f || (double)value < 0.1)
		{
			throw new ArgumentOutOfRangeException("value", "Value should be between .1 and 5.");
		}
		byte[] ramp = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			ramp[i] = (255.0 * Math.Pow((double)i / 255.0, value) + 0.5).ToByte();
		}
		int width = source.Width;
		int height = source.Height;
		FastBitmap bitmap = new FastBitmap(source);
		try
		{
			Parallel.For(0, height, delegate(int y)
			{
				for (int j = 0; j < width; j++)
				{
					Color pixel = bitmap.GetPixel(j, y);
					Color color = Color.FromArgb(pixel.A, ramp[pixel.R], ramp[pixel.G], ramp[pixel.B]);
					bitmap.SetPixel(j, y, color);
				}
			});
		}
		finally
		{
			if (bitmap != null)
			{
				((IDisposable)bitmap).Dispose();
			}
		}
		return (Bitmap)source;
	}

	public static Bitmap ToLinear(Image source)
	{
		byte[] ramp = LinearBytes.Value;
		int width = source.Width;
		int height = source.Height;
		FastBitmap bitmap = new FastBitmap(source);
		try
		{
			Parallel.For(0, height, delegate(int y)
			{
				for (int i = 0; i < width; i++)
				{
					Color pixel = bitmap.GetPixel(i, y);
					Color color = Color.FromArgb(pixel.A, ramp[pixel.R], ramp[pixel.G], ramp[pixel.B]);
					bitmap.SetPixel(i, y, color);
				}
			});
		}
		finally
		{
			if (bitmap != null)
			{
				((IDisposable)bitmap).Dispose();
			}
		}
		return (Bitmap)source;
	}

	public static Bitmap ToSRGB(Image source)
	{
		byte[] ramp = SRGBBytes.Value;
		int width = source.Width;
		int height = source.Height;
		FastBitmap bitmap = new FastBitmap(source);
		try
		{
			Parallel.For(0, height, delegate(int y)
			{
				for (int i = 0; i < width; i++)
				{
					Color pixel = bitmap.GetPixel(i, y);
					Color color = Color.FromArgb(pixel.A, ramp[pixel.R], ramp[pixel.G], ramp[pixel.B]);
					bitmap.SetPixel(i, y, color);
				}
			});
		}
		finally
		{
			if (bitmap != null)
			{
				((IDisposable)bitmap).Dispose();
			}
		}
		return (Bitmap)source;
	}

	private static byte[] GetLinearBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255.0 * SRGBToLinear((double)i / 255.0) + 0.5).ToByte();
		}
		return array;
	}

	private static byte[] GetSRGBBytes()
	{
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (255.0 * LinearToSRGB((double)i / 255.0) + 0.5).ToByte();
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double SRGBToLinear(double signal)
	{
		if (signal <= 0.04045)
		{
			return signal / 12.92;
		}
		return Math.Pow((signal + 0.055) / 1.055, 2.4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double LinearToSRGB(double signal)
	{
		if (signal <= 0.0031308049535603713)
		{
			return signal * 12.92;
		}
		return 1.055 * Math.Pow(signal, 5.0 / 12.0) - 0.055;
	}
}
