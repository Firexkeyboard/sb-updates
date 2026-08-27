using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class Resize : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public List<Size> RestrictedSizes { get; set; }

	public Resize()
	{
		RestrictedSizes = new List<Size>();
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			ResizeLayer obj = DynamicParameter;
			obj.RestrictedSizes = RestrictedSizes;
			Size value = default(Size);
			int.TryParse(Settings["MaxWidth"], NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			int.TryParse(Settings["MaxHeight"], NumberStyles.Any, CultureInfo.InvariantCulture, out var result2);
			value.Width = result;
			value.Height = result2;
			obj.MaxSize = value;
			bitmap = new Resizer(obj)
			{
				ImageFormat = factory.CurrentImageFormat,
				AnimationProcessMode = factory.AnimationProcessMode
			}.ResizeImage(image, factory.FixGamma);
			if (bitmap != image)
			{
				image.Dispose();
				image = bitmap;
				if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
				{
					factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);
					factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
				}
			}
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return image;
	}
}
