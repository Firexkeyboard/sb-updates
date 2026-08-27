namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class Laplacian5X5EdgeFilter : IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[5, 5]
	{
		{ -1.0, -1.0, -1.0, -1.0, -1.0 },
		{ -1.0, -1.0, -1.0, -1.0, -1.0 },
		{ -1.0, -1.0, 24.0, -1.0, -1.0 },
		{ -1.0, -1.0, -1.0, -1.0, -1.0 },
		{ -1.0, -1.0, -1.0, -1.0, -1.0 }
	};
}
