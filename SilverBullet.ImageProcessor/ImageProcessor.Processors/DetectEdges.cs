using System;
using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Filters.EdgeDetection;

namespace ImageProcessor.Processors;

public class DetectEdges : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public DetectEdges()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		Tuple<IEdgeFilter, bool> obj = DynamicParameter;
		IEdgeFilter item = obj.Item1;
		bool item2 = obj.Item2;
		try
		{
			ConvolutionFilter convolutionFilter = new ConvolutionFilter(item, item2);
			return (item is I2DEdgeFilter) ? convolutionFilter.Process2DFilter((Bitmap)image) : convolutionFilter.ProcessFilter((Bitmap)image);
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
