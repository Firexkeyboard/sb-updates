using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ImageProcessor.Processors;

public class ReduceNoise : IGraphicsProcessor
{
	private static object locker = new object();

	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap b = factory.Bitmap;
		ParallelOptions options = new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount - 1
		};
		int width = b.Width;
		int height = b.Height;
		Parallel.For(0, width, options, delegate(int x)
		{
			Parallel.For(0, height, options, delegate(int y)
			{
				Color pixel;
				lock (locker)
				{
					pixel = b.GetPixel(x, y);
				}
				if ((pixel.R + pixel.G + pixel.B) / 3 > 96)
				{
					lock (locker)
					{
						b.SetPixel(x, y, Color.White);
						return;
					}
				}
				lock (locker)
				{
					b.SetPixel(x, y, Color.Black);
				}
			});
		});
		return b;
	}

	private static void CalcArea(ref Bitmap bm, int x, int y, ref int size)
	{
		if (x >= 0 && x < bm.Width && y >= 0 && y < bm.Height)
		{
			Color pixel = bm.GetPixel(x, y);
			if (pixel.R != byte.MaxValue || pixel.G != byte.MaxValue || pixel.B != byte.MaxValue)
			{
				size++;
				bm.SetPixel(x, y, Color.White);
				CalcArea(ref bm, x - 1, y, ref size);
				CalcArea(ref bm, x, y - 1, ref size);
				CalcArea(ref bm, x + 1, y, ref size);
				CalcArea(ref bm, x, y + 1, ref size);
			}
		}
	}
}
