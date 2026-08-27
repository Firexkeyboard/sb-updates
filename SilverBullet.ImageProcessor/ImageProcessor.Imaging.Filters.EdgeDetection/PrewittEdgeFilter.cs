namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class PrewittEdgeFilter : I2DEdgeFilter, IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[3, 3]
	{
		{ -1.0, 0.0, 1.0 },
		{ -1.0, 0.0, 1.0 },
		{ -1.0, 0.0, 1.0 }
	};

	public double[,] VerticalGradientOperator => new double[3, 3]
	{
		{ 1.0, 1.0, 1.0 },
		{ 0.0, 0.0, 0.0 },
		{ -1.0, -1.0, -1.0 }
	};
}
