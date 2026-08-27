namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class KirschEdgeFilter : I2DEdgeFilter, IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[3, 3]
	{
		{ 5.0, 5.0, 5.0 },
		{ -3.0, 0.0, -3.0 },
		{ -3.0, -3.0, -3.0 }
	};

	public double[,] VerticalGradientOperator => new double[3, 3]
	{
		{ 5.0, -3.0, -3.0 },
		{ 5.0, 0.0, -3.0 },
		{ 5.0, -3.0, -3.0 }
	};
}
