using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers.Converters;
using SilverBullet.ImageProcessor;

namespace ImageProcessor.Processors;

public class Atomization : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Atomization()
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
			bitmap2 = new Bitmap(width, height);
			LockBitmap lockBitmap = new LockBitmap(bitmap);
			LockBitmap lockBitmap2 = new LockBitmap(bitmap2);
			lockBitmap.LockBits();
			lockBitmap2.LockBits();
			Random random = new Random();
			for (int i = 1; i < width - 1; i++)
			{
				for (int j = 1; j < height - 1; j++)
				{
					int num = random.Next(123456);
					int num2 = i + num % 19;
					int num3 = j + num % 19;
					if (num2 >= width)
					{
						num2 = width - 1;
					}
					if (num3 >= height)
					{
						num3 = height - 1;
					}
					Color pixel = lockBitmap.GetPixel(num2, num3);
					lockBitmap2.SetPixel(i, j, pixel);
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
