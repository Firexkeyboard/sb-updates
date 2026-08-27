using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageProcessor.Imaging.Helpers.Converters;

public static class ImageConverter
{
	private static object bufferLock = new object();

	public static byte[] ToByteArray(this Image image)
	{
		lock (bufferLock)
		{
			using MemoryStream memoryStream = new MemoryStream();
			image.Save(memoryStream, ImageFormat.Bmp);
			return memoryStream.ToArray();
		}
	}

	public static byte[] ToByteArray(this Image image, ImageFormat format)
	{
		lock (bufferLock)
		{
			using MemoryStream memoryStream = new MemoryStream();
			image.Save(memoryStream, format);
			return memoryStream.ToArray();
		}
	}

	public static byte[] BitmapToByteArray(Bitmap bitmap)
	{
		BitmapData bitmapData = null;
		try
		{
			bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
			int num = bitmapData.Stride * bitmap.Height;
			byte[] array = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array, 0, num);
			return array;
		}
		finally
		{
			if (bitmapData != null)
			{
				bitmap.UnlockBits(bitmapData);
			}
		}
	}

	public static Image ToImage(this Bitmap bitmap)
	{
		return bitmap;
	}

	public static Bitmap ToBitmap(this Image image)
	{
		return (Bitmap)image;
	}
}
