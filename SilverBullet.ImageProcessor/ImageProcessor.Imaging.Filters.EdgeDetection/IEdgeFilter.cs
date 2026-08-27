namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public interface IEdgeFilter
{
	double[,] HorizontalGradientOperator { get; }
}
