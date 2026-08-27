using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;
using ImageProcessor.Imaging.Helpers.Converters;

namespace ImageProcessor.Processors;

public class Expend : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Expend()
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
			for (int i = 1; i < width - 1; i++)
			{
				for (int j = 1; j < height - 1; j++)
				{
					if (bitmap.GetPixel(i, j).R == 0)
					{
						continue;
					}
					bool[] roundPixel = ImageMaths.GetRoundPixel(bitmap, i, j);
					for (int k = 0; k < roundPixel.Length; k++)
					{
						if (roundPixel[k])
						{
							bitmap2.SetPixel(i, j, Color.FromArgb(0, 0, 0));
							break;
						}
					}
				}
			}
			return bitmap2;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
