using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageProcessor.Processors;

public class FaceWhiten : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = factory.Bitmap;
		for (int i = 1; i < bitmap.Width - 1; i++)
		{
			for (int j = 1; j < bitmap.Height - 1; j++)
			{
				Color pixel = bitmap.GetPixel(i, j);
				int r = pixel.R;
				int g = pixel.G;
				int b = pixel.B;
				if (r > g && g > b && Math.Abs(r - g) > 30)
				{
					int num = 30;
					int num2 = Convert.ToInt32((((double)(int)pixel.R / 255.0 - 0.5) * 1.2 + 0.5) * 255.0) + num;
					int num3 = Convert.ToInt32((((double)(int)pixel.G / 255.0 - 0.5) * 1.2 + 0.5) * 255.0) + num;
					int num4 = Convert.ToInt32((((double)(int)pixel.B / 255.0 - 0.5) * 1.1 + 0.5) * 255.0) + num;
					if (num2 < 0)
					{
						num2 = 0;
					}
					else if (num2 > 255)
					{
						num2 = 255;
					}
					if (num4 < 0)
					{
						num4 = 0;
					}
					else if (num4 > 255)
					{
						num4 = 255;
					}
					if (num3 < 0)
					{
						num3 = 0;
					}
					else if (num3 > 255)
					{
						num3 = 255;
					}
					Color color = Color.FromArgb(num2, num3, num4);
					bitmap.SetPixel(i, j, color);
				}
			}
		}
		return bitmap;
	}
}
