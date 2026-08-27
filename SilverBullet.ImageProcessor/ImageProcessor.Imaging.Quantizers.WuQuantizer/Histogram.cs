using System;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

public class Histogram
{
	internal readonly ColorMoment[,,,] Moments;

	private const int SideSize = 33;

	public Histogram()
	{
		Moments = new ColorMoment[33, 33, 33, 33];
	}

	internal void Clear()
	{
		Array.Clear(Moments, 0, 1185921);
	}
}
