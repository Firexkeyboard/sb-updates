using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using OpenCvSharp;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Median : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		try
		{
			int ksize = (int)DynamicParameter;
			return ImageHelper.OpenCvProcessor(factory.Bitmap, (Mat src) => src.MedianBlur(ksize));
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
