using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class Quality : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Quality()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		try
		{
			factory.CurrentImageFormat.Quality = DynamicParameter;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return factory.Image;
	}
}
