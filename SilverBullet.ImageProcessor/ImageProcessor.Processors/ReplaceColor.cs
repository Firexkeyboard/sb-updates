using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class ReplaceColor : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public ReplaceColor()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			Tuple<Color, Color, int> obj = DynamicParameter;
			Color item = obj.Item1;
			Color item2 = obj.Item2;
			byte originalR = item.R;
			byte originalG = item.G;
			byte originalB = item.B;
			byte originalA = item.A;
			byte replacementR = item2.R;
			byte replacementG = item2.G;
			byte replacementB = item2.B;
			byte replacementA = item2.A;
			int item3 = obj.Item3;
			byte minR = (originalR - item3).ToByte();
			byte minG = (originalG - item3).ToByte();
			byte minB = (originalB - item3).ToByte();
			byte maxR = (originalR + item3).ToByte();
			byte maxG = (originalG + item3).ToByte();
			byte maxB = (originalB + item3).ToByte();
			bitmap = new Bitmap(image);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			int width = image.Width;
			int height = image.Height;
			FastBitmap fastBitmap = new FastBitmap(bitmap);
			try
			{
				Parallel.For(0, height, delegate(int y)
				{
					for (int i = 0; i < width; i++)
					{
						Color pixel = fastBitmap.GetPixel(i, y);
						byte r = pixel.R;
						byte g = pixel.G;
						byte b = pixel.B;
						byte a = pixel.A;
						if (ImageMaths.InRange(r, minR, maxR) && ImageMaths.InRange(g, minG, maxG) && ImageMaths.InRange(b, minB, maxB))
						{
							byte red = (originalR - r + replacementR).ToByte();
							byte green = (originalG - g + replacementG).ToByte();
							byte blue = (originalB - b + replacementB).ToByte();
							byte alpha = a;
							if (originalA != replacementA)
							{
								alpha = replacementA;
							}
							fastBitmap.SetPixel(i, y, Color.FromArgb(alpha, red, green, blue));
						}
					}
				});
			}
			finally
			{
				if (fastBitmap != null)
				{
					((IDisposable)fastBitmap).Dispose();
				}
			}
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
