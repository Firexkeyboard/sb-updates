using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class Invert : IGraphicsProcessor
{
	public dynamic DynamicParameter
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public Dictionary<string, string> Settings
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
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
			int num2 = bitmap.Width * 3;
			for (int i = 0; i < bitmap.Height; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					*ptr = (byte)(255 - *ptr);
					ptr++;
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
