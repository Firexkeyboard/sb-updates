using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

internal class ImageBuffer
{
	public Bitmap Image { get; }

	public IEnumerable<Color32[]> PixelLines
	{
		get
		{
			int width = Image.Width;
			int height = Image.Height;
			Color32[] pixels = new Color32[width];
			using FastBitmap bitmap = new FastBitmap(Image);
			for (int y = 0; y < height; y++)
			{
				for (int i = 0; i < width; i++)
				{
					Color pixel = bitmap.GetPixel(i, y);
					pixels[i] = new Color32(pixel.A, pixel.R, pixel.G, pixel.B);
				}
				yield return pixels;
			}
		}
	}

	public ImageBuffer(Bitmap image)
	{
		Image = image;
	}

	public void UpdatePixelIndexes(IEnumerable<byte[]> lineIndexes)
	{
		int width = Image.Width;
		int height = Image.Height;
		int num = 0;
		BitmapData bitmapData = Image.LockBits(Rectangle.FromLTRB(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
		try
		{
			IntPtr scan = bitmapData.Scan0;
			int stride = bitmapData.Stride;
			foreach (byte[] lineIndex in lineIndexes)
			{
				Marshal.Copy(lineIndex, 0, IntPtr.Add(scan, stride * num), width);
				if (++num >= height)
				{
					break;
				}
			}
		}
		finally
		{
			Image.UnlockBits(bitmapData);
		}
	}
}
