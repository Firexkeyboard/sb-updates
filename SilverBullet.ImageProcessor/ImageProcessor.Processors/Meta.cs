using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class Meta : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Meta()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		try
		{
			factory.PreserveExifData = DynamicParameter;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return factory.Image;
	}
}
