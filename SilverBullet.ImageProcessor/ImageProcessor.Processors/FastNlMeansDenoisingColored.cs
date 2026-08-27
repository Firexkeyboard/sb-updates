using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Helpers.Converters;
using OpenBullet.ImageProcessor.Layers;
using OpenCvSharp;

namespace ImageProcessor.Processors;

public class FastNlMeansDenoisingColored : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		FastNlMeansDenoisingColoredLayer fastNlMeansDenoisingColoredLayer = (FastNlMeansDenoisingColoredLayer)DynamicParameter;
		try
		{
			using Mat mat = Mat.FromImageData(factory.Bitmap.ToByteArray());
			Cv2.FastNlMeansDenoisingColored(mat, mat, fastNlMeansDenoisingColoredLayer.Strength, fastNlMeansDenoisingColoredLayer.ColorStrength);
			bitmap = mat.ToBitmap();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
