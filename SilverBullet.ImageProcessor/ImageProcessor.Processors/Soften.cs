using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using SilverBullet.ImageProcessor;

namespace ImageProcessor.Processors;

public class Soften : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = factory.Bitmap;
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
			int[] array = new int[9] { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
			for (int i = 1; i < width - 1; i++)
			{
				for (int j = 1; j < height - 1; j++)
				{
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					int num4 = 0;
					for (int k = -1; k <= 1; k++)
					{
						for (int l = -1; l <= 1; l++)
						{
							Color pixel = lockBitmap.GetPixel(i + l, j + k);
							num += pixel.R * array[num4];
							num2 += pixel.G * array[num4];
							num3 += pixel.B * array[num4];
							num4++;
						}
					}
					num /= 16;
					num2 /= 16;
					num3 /= 16;
					num = ((num > 255) ? 255 : num);
					num = ((num >= 0) ? num : 0);
					num2 = ((num2 > 255) ? 255 : num2);
					num2 = ((num2 >= 0) ? num2 : 0);
					num3 = ((num3 > 255) ? 255 : num3);
					num3 = ((num3 >= 0) ? num3 : 0);
					lockBitmap2.SetPixel(i - 1, j - 1, Color.FromArgb(num, num2, num3));
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
