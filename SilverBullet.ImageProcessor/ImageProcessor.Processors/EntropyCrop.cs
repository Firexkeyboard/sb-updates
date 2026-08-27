using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Filters.Binarization;
using ImageProcessor.Imaging.Filters.EdgeDetection;
using ImageProcessor.Imaging.Helpers;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class EntropyCrop : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public EntropyCrop()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		Image image = factory.Image;
		byte threshold = DynamicParameter;
		try
		{
			bitmap2 = new ConvolutionFilter(new SobelEdgeFilter(), greyscale: true).Process2DFilter(image);
			bitmap2 = new BinaryThreshold(threshold).ProcessFilter(bitmap2);
			Rectangle filteredBoundingRectangle = ImageMaths.GetFilteredBoundingRectangle(bitmap2, 0);
			bitmap2.Dispose();
			bitmap = new Bitmap(filteredBoundingRectangle.Width, filteredBoundingRectangle.Height, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.DrawImage(image, new Rectangle(0, 0, filteredBoundingRectangle.Width, filteredBoundingRectangle.Height), filteredBoundingRectangle.X, filteredBoundingRectangle.Y, filteredBoundingRectangle.Width, filteredBoundingRectangle.Height, GraphicsUnit.Pixel);
			}
			image.Dispose();
			image = bitmap;
			if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
			{
				factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);
				factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
			}
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return image;
	}
}
