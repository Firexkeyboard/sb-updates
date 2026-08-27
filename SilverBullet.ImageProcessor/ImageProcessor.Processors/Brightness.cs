using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Brightness : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Brightness()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			int threshold = (int)DynamicParameter;
			return Adjustments.Brightness(image, threshold, null);
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
