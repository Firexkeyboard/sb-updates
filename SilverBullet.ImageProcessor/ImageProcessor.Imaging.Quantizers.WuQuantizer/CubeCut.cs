namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

internal readonly struct CubeCut
{
	public readonly byte? Position;

	public readonly float Value;

	public CubeCut(byte? cutPoint, float result)
	{
		Position = cutPoint;
		Value = result;
	}
}
