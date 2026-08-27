using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Filters.ObjectDetection;
using ImageProcessor.Imaging.Filters.Photo;

namespace ImageProcessor.Processors;

public class DetectObjects : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public DetectObjects()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		Image image = factory.Image;
		try
		{
			HaarCascade cascade = DynamicParameter;
			bitmap2 = new Bitmap(image.Width, image.Height);
			bitmap2.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			bitmap2 = MatrixFilters.GreyScale.TransformImage(image, bitmap2);
			Rectangle[] rects = new HaarObjectDetector(cascade)
			{
				SearchMode = ObjectDetectorSearchMode.NoOverlap,
				ScalingMode = ObjectDetectorScalingMode.GreaterToSmaller,
				ScalingFactor = 1.5f
			}.ProcessFrame(bitmap2);
			bitmap2.Dispose();
			bitmap = new Bitmap(image);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				using Pen pen = new Pen(Color.White);
				pen.Width = 4f;
				graphics.DrawRectangles(pen, rects);
			}
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
