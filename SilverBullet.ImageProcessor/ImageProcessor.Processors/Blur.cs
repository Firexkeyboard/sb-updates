using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers.Converters;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Blur : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Blur()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		try
		{
			ConvMatrix convMatrix = new ConvMatrix();
			convMatrix.SetAll(1);
			int num = (convMatrix.Pixel = (int)DynamicParameter);
			convMatrix.TopMid = (convMatrix.MidLeft = (convMatrix.MidRight = (convMatrix.BottomMid = 2)));
			convMatrix.Factor = num + 12;
			bitmap = ImageHelper.Conv3x3(factory.Image.ToBitmap(), convMatrix);
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
