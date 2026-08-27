using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Filters.Artistic;

public class OilPaintingFilter
{
	private int levels;

	private int brushSize;

	public int Levels
	{
		get
		{
			return levels;
		}
		set
		{
			if (value > 0)
			{
				levels = value;
			}
		}
	}

	public int BrushSize
	{
		get
		{
			return brushSize;
		}
		set
		{
			if (value > 0)
			{
				brushSize = value;
			}
		}
	}

	public OilPaintingFilter(int levels, int brushSize)
	{
		this.levels = levels;
		this.brushSize = brushSize;
	}

	public Bitmap ApplyFilter(Bitmap source)
	{
		int width = source.Width;
		int height = source.Height;
		int radius = brushSize >> 1;
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		FastBitmap sourceBitmap = new FastBitmap(source);
		try
		{
			FastBitmap destinationBitmap = new FastBitmap(bitmap);
			try
			{
				Parallel.For(0, height, delegate(int y)
				{
					for (int i = 0; i < width; i++)
					{
						int num = 0;
						int num2 = 0;
						int[] array = new int[levels];
						int[] array2 = new int[levels];
						int[] array3 = new int[levels];
						int[] array4 = new int[levels];
						byte alpha = byte.MaxValue;
						for (int j = 0; j <= radius; j++)
						{
							int num3 = j - radius;
							int num4 = y + num3;
							if (num4 >= 0)
							{
								if (num4 >= height)
								{
									break;
								}
								for (int k = 0; k <= radius; k++)
								{
									int num5 = k - radius;
									int num6 = i + num5;
									if (num6 >= 0 && num6 < width)
									{
										Color pixel = sourceBitmap.GetPixel(num6, num4);
										byte b = pixel.B;
										byte g = pixel.G;
										byte r = pixel.R;
										alpha = pixel.A;
										int num7 = (int)Math.Round((double)(b + g + r) / 3.0 * (double)(levels - 1) / 255.0);
										array[num7]++;
										array2[num7] += b;
										array3[num7] += g;
										array4[num7] += r;
										if (array[num7] > num)
										{
											num = array[num7];
											num2 = num7;
										}
									}
								}
							}
						}
						byte blue = Math.Abs(array2[num2] / num).ToByte();
						byte green = Math.Abs(array3[num2] / num).ToByte();
						byte red = Math.Abs(array4[num2] / num).ToByte();
						destinationBitmap.SetPixel(i, y, Color.FromArgb(alpha, red, green, blue));
					}
				});
				return bitmap;
			}
			finally
			{
				if (destinationBitmap != null)
				{
					((IDisposable)destinationBitmap).Dispose();
				}
			}
		}
		finally
		{
			if (sourceBitmap != null)
			{
				((IDisposable)sourceBitmap).Dispose();
			}
		}
	}
}
