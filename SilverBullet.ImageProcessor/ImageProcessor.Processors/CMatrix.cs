using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class CMatrix : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public CMatrix()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		return Apply(factory, (ColorMatrix)DynamicParameter);
	}

	public static Image Apply(ImageFactory factory, ColorMatrix colorMatrix)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		try
		{
			bitmap = ImageMaths.GetArgbCopy(factory.Image);
			bitmap2 = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
			using Graphics graphics = Graphics.FromImage(bitmap2);
			ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(colorMatrix);
			graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with CMatrix", innerException);
		}
		bitmap.Dispose();
		return bitmap2;
	}
}
