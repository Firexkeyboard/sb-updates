using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class Resolution : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Resolution()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			Tuple<int, int, PropertyTagResolutionUnit> tuple = DynamicParameter;
			if (tuple.Item3 == PropertyTagResolutionUnit.Cm)
			{
				float xDpi = (float)tuple.Item1 / 0.39370078f;
				float yDpi = (float)tuple.Item2 / 0.39370078f;
				((Bitmap)image).SetResolution(xDpi, yDpi);
			}
			else
			{
				((Bitmap)image).SetResolution(tuple.Item1, tuple.Item2);
			}
			if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
			{
				Rational<uint> value = new Rational<uint>((uint)tuple.Item1, 1u);
				factory.SetPropertyItem(ExifPropertyTag.XResolution, value);
				Rational<uint> value2 = new Rational<uint>((uint)tuple.Item2, 1u);
				factory.SetPropertyItem(ExifPropertyTag.YResolution, value2);
				ushort item = (ushort)tuple.Item3;
				factory.SetPropertyItem(ExifPropertyTag.ResolutionUnit, item);
			}
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return image;
	}
}
