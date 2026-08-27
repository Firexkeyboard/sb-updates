namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class ScharrEdgeFilter : I2DEdgeFilter, IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[3, 3]
	{
		{ -3.0, 0.0, 3.0 },
		{ -10.0, 0.0, 10.0 },
		{ -3.0, 0.0, 3.0 }
	};

	public double[,] VerticalGradientOperator => new double[3, 3]
	{
		{ 3.0, 10.0, 3.0 },
		{ 0.0, 0.0, 0.0 },
		{ -3.0, -10.0, -3.0 }
	};
}
