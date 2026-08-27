using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

internal struct ColorMoment
{
	public long Alpha;

	public long Blue;

	public long Green;

	public float Moment;

	public long Red;

	public int Weight;

	public static ColorMoment operator +(ColorMoment firstAddend, ColorMoment secondAddend)
	{
		firstAddend.Alpha += secondAddend.Alpha;
		firstAddend.Red += secondAddend.Red;
		firstAddend.Green += secondAddend.Green;
		firstAddend.Blue += secondAddend.Blue;
		firstAddend.Weight += secondAddend.Weight;
		firstAddend.Moment += secondAddend.Moment;
		return firstAddend;
	}

	public static ColorMoment operator -(ColorMoment minuend, ColorMoment subtrahend)
	{
		minuend.Alpha -= subtrahend.Alpha;
		minuend.Red -= subtrahend.Red;
		minuend.Green -= subtrahend.Green;
		minuend.Blue -= subtrahend.Blue;
		minuend.Weight -= subtrahend.Weight;
		minuend.Moment -= subtrahend.Moment;
		return minuend;
	}

	public static ColorMoment operator -(ColorMoment moment)
	{
		moment.Alpha = -moment.Alpha;
		moment.Red = -moment.Red;
		moment.Green = -moment.Green;
		moment.Blue = -moment.Blue;
		moment.Weight = -moment.Weight;
		moment.Moment = 0f - moment.Moment;
		return moment;
	}

	public void Add(Color32 pixel)
	{
		byte a = pixel.A;
		byte r = pixel.R;
		byte g = pixel.G;
		byte b = pixel.B;
		Alpha += a;
		Red += r;
		Green += g;
		Blue += b;
		Weight++;
		Moment += a * a + r * r + g * g + b * b;
	}

	public void AddFast(ref ColorMoment moment)
	{
		Alpha += moment.Alpha;
		Red += moment.Red;
		Green += moment.Green;
		Blue += moment.Blue;
		Weight += moment.Weight;
		Moment += moment.Moment;
	}

	public long Amplitude()
	{
		return Alpha * Alpha + Red * Red + Green * Green + Blue * Blue;
	}

	public float Variance()
	{
		float num = Moment - (float)Amplitude() / (float)Weight;
		if (!float.IsNaN(num))
		{
			return num;
		}
		return 0f;
	}

	public long WeightedDistance()
	{
		return Amplitude() / Weight;
	}
}
