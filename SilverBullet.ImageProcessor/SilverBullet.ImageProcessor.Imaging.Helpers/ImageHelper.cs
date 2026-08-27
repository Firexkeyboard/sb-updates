using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers.Converters;
using OpenCvSharp;

namespace SilverBullet.ImageProcessor.Imaging.Helpers;

public class ImageHelper
{
	internal unsafe static Bitmap Conv3x3(Bitmap b, ConvMatrix m)
	{
		if (m.Factor == 0)
		{
			return b;
		}
		Bitmap bitmap = (Bitmap)b.Clone();
		BitmapData bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
		BitmapData bitmapData2 = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
		int stride = bitmapData.Stride;
		int num = stride * 2;
		IntPtr scan = bitmapData.Scan0;
		IntPtr scan2 = bitmapData2.Scan0;
		byte* ptr = (byte*)(void*)scan;
		byte* ptr2 = (byte*)(void*)scan2;
		int num2 = stride - b.Width * 3;
		int num3 = b.Width - 2;
		int num4 = b.Height - 2;
		for (int i = 0; i < num4; i++)
		{
			for (int j = 0; j < num3; j++)
			{
				int num5 = (ptr2[2] * m.TopLeft + ptr2[5] * m.TopMid + ptr2[8] * m.TopRight + ptr2[2 + stride] * m.MidLeft + ptr2[5 + stride] * m.Pixel + ptr2[8 + stride] * m.MidRight + ptr2[2 + num] * m.BottomLeft + ptr2[5 + num] * m.BottomMid + ptr2[8 + num] * m.BottomRight) / m.Factor + m.Offset;
				if (num5 < 0)
				{
					num5 = 0;
				}
				if (num5 > 255)
				{
					num5 = 255;
				}
				ptr[5 + stride] = (byte)num5;
				num5 = (ptr2[1] * m.TopLeft + ptr2[4] * m.TopMid + ptr2[7] * m.TopRight + ptr2[1 + stride] * m.MidLeft + ptr2[4 + stride] * m.Pixel + ptr2[7 + stride] * m.MidRight + ptr2[1 + num] * m.BottomLeft + ptr2[4 + num] * m.BottomMid + ptr2[7 + num] * m.BottomRight) / m.Factor + m.Offset;
				if (num5 < 0)
				{
					num5 = 0;
				}
				if (num5 > 255)
				{
					num5 = 255;
				}
				ptr[4 + stride] = (byte)num5;
				num5 = (*ptr2 * m.TopLeft + ptr2[3] * m.TopMid + ptr2[6] * m.TopRight + ptr2[stride] * m.MidLeft + ptr2[3 + stride] * m.Pixel + ptr2[6 + stride] * m.MidRight + ptr2[num] * m.BottomLeft + ptr2[3 + num] * m.BottomMid + ptr2[6 + num] * m.BottomRight) / m.Factor + m.Offset;
				if (num5 < 0)
				{
					num5 = 0;
				}
				if (num5 > 255)
				{
					num5 = 255;
				}
				ptr[3 + stride] = (byte)num5;
				ptr += 3;
				ptr2 += 3;
			}
			ptr += num2;
			ptr2 += num2;
		}
		b.UnlockBits(bitmapData);
		bitmap.UnlockBits(bitmapData2);
		return b;
	}

	internal static Bitmap OpenCvProcessor(Bitmap bitmap, Func<Mat, Mat> func, ImreadModes imreadModes = ImreadModes.Color)
	{
		using Mat arg = Mat.FromImageData(bitmap.ToByteArray(), imreadModes);
		using Mat mat = func?.Invoke(arg);
		bitmap = mat.ToBitmap().Clone() as Bitmap;
		return bitmap;
	}
}
