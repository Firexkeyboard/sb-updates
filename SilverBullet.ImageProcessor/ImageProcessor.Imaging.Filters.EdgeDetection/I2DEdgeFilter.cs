namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public interface I2DEdgeFilter : IEdgeFilter
{
	double[,] VerticalGradientOperator { get; }
}
