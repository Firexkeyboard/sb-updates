using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class Grayscale : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Grayscale()
	{
		Settings = new Dictionary<string, string>();
	}

	public unsafe Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = factory.Bitmap;
		try
		{
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			int stride = bitmapData.Stride;
			byte* ptr = (byte*)(void*)bitmapData.Scan0;
			int num = stride - bitmap.Width * 3;
			for (int i = 0; i < bitmap.Height; i++)
			{
				for (int j = 0; j < bitmap.Width; j++)
				{
					byte b = *ptr;
					byte b2 = ptr[1];
					byte b3 = ptr[2];
					*ptr = (ptr[1] = (ptr[2] = (byte)(0.299 * (double)(int)b3 + 0.587 * (double)(int)b2 + 0.114 * (double)(int)b)));
					ptr += 3;
				}
				ptr += num;
			}
			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
