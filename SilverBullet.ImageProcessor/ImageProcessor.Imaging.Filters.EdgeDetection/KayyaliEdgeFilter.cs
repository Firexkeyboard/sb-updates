namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class KayyaliEdgeFilter : I2DEdgeFilter, IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[3, 3]
	{
		{ 6.0, 0.0, -6.0 },
		{ 0.0, 0.0, 0.0 },
		{ -6.0, 0.0, 6.0 }
	};

	public double[,] VerticalGradientOperator => new double[3, 3]
	{
		{ -6.0, 0.0, 6.0 },
		{ 0.0, 0.0, 0.0 },
		{ 6.0, 0.0, -6.0 }
	};
}
