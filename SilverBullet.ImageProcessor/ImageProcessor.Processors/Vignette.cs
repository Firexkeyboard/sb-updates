using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Vignette : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Vignette()
	{
		DynamicParameter = Color.Black;
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			Color baseColor = (Color)DynamicParameter;
			return Effects.Vignette(image, baseColor, null);
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
