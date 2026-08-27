using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class MakeTransparent : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		try
		{
			Bitmap bitmap = factory.Bitmap;
			bitmap.MakeTransparent(bitmap.GetPixel(1, 1));
			return bitmap;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
