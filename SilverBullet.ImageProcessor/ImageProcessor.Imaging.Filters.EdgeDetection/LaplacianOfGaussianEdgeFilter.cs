namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class LaplacianOfGaussianEdgeFilter : IEdgeFilter
{
	public double[,] HorizontalGradientOperator => new double[5, 5]
	{
		{ 0.0, 0.0, -1.0, 0.0, 0.0 },
		{ 0.0, -1.0, -2.0, -1.0, 0.0 },
		{ -1.0, -2.0, 16.0, -2.0, -1.0 },
		{ 0.0, -1.0, -2.0, -1.0, 0.0 },
		{ 0.0, 0.0, -1.0, 0.0, 0.0 }
	};
}
