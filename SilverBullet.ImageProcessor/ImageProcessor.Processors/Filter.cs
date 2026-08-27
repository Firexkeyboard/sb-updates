using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Filters.Photo;

namespace ImageProcessor.Processors;

public class Filter : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Filter()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			bitmap = ((IMatrixFilter)DynamicParameter).TransformImage(image, bitmap);
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
