namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class RobertsCrossEdgeFilter : I2DEdgeFilter, IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[2, 2]
	{
		{ 1.0, 0.0 },
		{ 0.0, -1.0 }
	};

	public double[,] VerticalGradientOperator => new double[2, 2]
	{
		{ 0.0, 1.0 },
		{ -1.0, 0.0 }
	};
}
