using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Processors;

public class AutoRotate : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public AutoRotate()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			if (!factory.PreserveExifData && factory.ExifPropertyItems.ContainsKey(274))
			{
				switch (factory.ExifPropertyItems[274].Value[0])
				{
				case 8:
					image.RotateFlip(RotateFlipType.Rotate270FlipNone);
					break;
				case 7:
					image.RotateFlip(RotateFlipType.Rotate270FlipX);
					break;
				case 6:
					image.RotateFlip(RotateFlipType.Rotate90FlipNone);
					break;
				case 5:
					image.RotateFlip(RotateFlipType.Rotate90FlipX);
					break;
				case 3:
					image.RotateFlip(RotateFlipType.Rotate180FlipNone);
					break;
				case 2:
					image.RotateFlip(RotateFlipType.RotateNoneFlipX);
					break;
				}
				Image image2 = image.Copy(factory.AnimationProcessMode, image.PixelFormat);
				image.Dispose();
				return factory.Image = image2;
			}
			return image;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
