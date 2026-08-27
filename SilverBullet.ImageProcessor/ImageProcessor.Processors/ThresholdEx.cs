using System.Collections.Generic;
using System.Drawing;
using OpenBullet.ImageProcessor.Layers;
using OpenCvSharp;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class ThresholdEx : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		ThresholdLayer layer = (ThresholdLayer)DynamicParameter;
		return ImageHelper.OpenCvProcessor(factory.Bitmap, (Mat src) => src.Threshold(layer.Thresh, layer.MaxValue, layer.ThresholdType));
	}
}
