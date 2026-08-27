using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;

namespace ImageProcessor.Processors;

public class GaussianSharpen : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public GaussianSharpen()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap source = (Bitmap)factory.Image;
		try
		{
			GaussianLayer gaussianLayer = DynamicParameter;
			Convolution obj = new Convolution(gaussianLayer.Sigma)
			{
				Threshold = gaussianLayer.Threshold
			};
			double[,] kernel = obj.CreateGuassianSharpenFilter(gaussianLayer.Size);
			return obj.ProcessKernel(source, kernel, factory.FixGamma);
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
