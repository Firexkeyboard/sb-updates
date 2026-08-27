using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using OpenBullet.ImageProcessor.Layers;
using OpenCvSharp;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class MorphologyEx : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		MorphologyLayer layer = (MorphologyLayer)DynamicParameter;
		Bitmap bitmap = null;
		try
		{
			bitmap = ((layer.Kernel == null) ? ImageHelper.OpenCvProcessor(factory.Bitmap, delegate(Mat src)
			{
				MorphTypes morphTypes = layer.MorphTypes;
				int iterations = layer.Iterations;
				BorderTypes borderTypes = layer.BorderTypes;
				return src.MorphologyEx(morphTypes, null, null, iterations, borderTypes, null);
			}) : ImageHelper.OpenCvProcessor(factory.Bitmap, delegate(Mat src)
			{
				MorphTypes morphTypes2 = layer.MorphTypes;
				InputArray element = layer.Kernel;
				int iterations2 = layer.Iterations;
				BorderTypes borderTypes2 = layer.BorderTypes;
				return src.MorphologyEx(morphTypes2, element, null, iterations2, borderTypes2, null);
			}));
		}
		catch (Exception innerException)
		{
			layer.Kernel?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		layer.Kernel?.Dispose();
		return bitmap;
	}
}
