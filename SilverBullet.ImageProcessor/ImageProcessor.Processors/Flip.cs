using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class Flip : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Flip()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			RotateFlipType rotateFlipType = DynamicParameter;
			image.RotateFlip(rotateFlipType);
			if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
			{
				factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);
				factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
			}
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return image;
	}
}
