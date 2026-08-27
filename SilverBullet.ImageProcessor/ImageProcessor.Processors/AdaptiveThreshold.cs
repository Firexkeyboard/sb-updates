using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;
using OpenBullet.ImageProcessor.Layers;
using OpenCvSharp;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class AdaptiveThreshold : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		AdaptiveThresholdLayer layer = (AdaptiveThresholdLayer)DynamicParameter;
		Bitmap newBmp = null;
		try
		{
			newBmp = ImageHelper.OpenCvProcessor(factory.Bitmap, (Mat src) => src.AdaptiveThreshold(layer.MaxValue, layer.AdaptiveMethod, layer.ThresholdType, layer.BlockSize, layer.C));
		}
		catch (OpenCVException ex)
		{
			if (ex.Message == "src.type() == CV_8UC1")
			{
				newBmp = ImageHelper.OpenCvProcessor(factory.Bitmap, delegate(Mat src)
				{
					Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);
					newBmp = src.ToBitmap();
					return src.AdaptiveThreshold(layer.MaxValue, layer.AdaptiveMethod, layer.ThresholdType, layer.BlockSize, layer.C);
				});
			}
		}
		catch (Exception innerException)
		{
			newBmp?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return newBmp;
	}
}
