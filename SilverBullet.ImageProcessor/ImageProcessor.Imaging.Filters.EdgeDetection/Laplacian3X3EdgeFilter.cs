namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class Laplacian3X3EdgeFilter : IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[3, 3]
	{
		{ -1.0, -1.0, -1.0 },
		{ -1.0, 8.0, -1.0 },
		{ -1.0, -1.0, -1.0 }
	};
}
