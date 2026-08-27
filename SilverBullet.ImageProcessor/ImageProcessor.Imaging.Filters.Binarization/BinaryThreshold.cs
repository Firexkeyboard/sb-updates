using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ImageProcessor.Imaging.Filters.Binarization;

public class BinaryThreshold
{
	public byte Threshold { get; set; }

	public BinaryThreshold(byte threshold = 10)
	{
		Threshold = threshold;
	}

	public Bitmap ProcessFilter(Bitmap source)
	{
		int width = source.Width;
		int height = source.Height;
		FastBitmap sourceBitmap = new FastBitmap(source);
		try
		{
			Parallel.For(0, height, delegate(int y)
			{
				for (int i = 0; i < width; i++)
				{
					Color pixel = sourceBitmap.GetPixel(i, y);
					sourceBitmap.SetPixel(i, y, (pixel.B >= Threshold) ? Color.White : Color.Black);
				}
			});
			return source;
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
