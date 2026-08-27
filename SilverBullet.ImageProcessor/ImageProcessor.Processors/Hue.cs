using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Processors;

public class Hue : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Hue()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			Tuple<int, bool> obj = DynamicParameter;
			int item = obj.Item1;
			bool item2 = obj.Item2;
			if (item == 0 && item2)
			{
				return image;
			}
			int width = image.Width;
			int height = image.Height;
			using (FastBitmap fastBitmap = new FastBitmap(image))
			{
				if (!item2)
				{
					for (int i = 0; i < height; i++)
					{
						for (int j = 0; j < width; j++)
						{
							HslaColor hslaColor = HslaColor.FromColor(fastBitmap.GetPixel(j, i));
							HslaColor hslaColor2 = HslaColor.FromHslaColor((float)item / 360f, hslaColor.S, hslaColor.L, hslaColor.A);
							fastBitmap.SetPixel(j, i, hslaColor2);
						}
					}
				}
				else
				{
					for (int k = 0; k < height; k++)
					{
						for (int l = 0; l < width; l++)
						{
							HslaColor hslaColor3 = HslaColor.FromColor(fastBitmap.GetPixel(l, k));
							HslaColor hslaColor4 = HslaColor.FromHslaColor((hslaColor3.H + (float)item / 360f) % 1f, hslaColor3.S, hslaColor3.L, hslaColor3.A);
							fastBitmap.SetPixel(l, k, hslaColor4);
						}
					}
				}
			}
			return image;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
