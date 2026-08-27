using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers.Converters;
using SilverBullet.ImageProcessor;

namespace ImageProcessor.Processors;

public class Embossment : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Embossment()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = factory.Image.ToBitmap();
		Bitmap bitmap2 = null;
		try
		{
			int height = bitmap.Height;
			int width = bitmap.Width;
			Bitmap bitmap3 = new Bitmap(width, height);
			LockBitmap lockBitmap = new LockBitmap(bitmap);
			LockBitmap lockBitmap2 = new LockBitmap(bitmap3);
			lockBitmap.LockBits();
			lockBitmap2.LockBits();
			for (int i = 0; i < width - 1; i++)
			{
				for (int j = 0; j < height - 1; j++)
				{
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					Color pixel = lockBitmap.GetPixel(i, j);
					Color pixel2 = lockBitmap.GetPixel(i + 1, j + 1);
					num = Math.Abs(pixel.R - pixel2.R + 128);
					num2 = Math.Abs(pixel.G - pixel2.G + 128);
					num3 = Math.Abs(pixel.B - pixel2.B + 128);
					if (num > 255)
					{
						num = 255;
					}
					if (num < 0)
					{
						num = 0;
					}
					if (num2 > 255)
					{
						num2 = 255;
					}
					if (num2 < 0)
					{
						num2 = 0;
					}
					if (num3 > 255)
					{
						num3 = 255;
					}
					if (num3 < 0)
					{
						num3 = 0;
					}
					lockBitmap2.SetPixel(i, j, Color.FromArgb(num, num2, num3));
				}
			}
			lockBitmap.UnlockBits();
			lockBitmap2.UnlockBits();
			return bitmap2;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
