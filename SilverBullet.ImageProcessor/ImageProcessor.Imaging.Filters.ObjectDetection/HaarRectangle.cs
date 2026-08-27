using System;
using System.Globalization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
public class HaarRectangle : ICloneable
{
	public int X { get; set; }

	public int Y { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	public float Weight { get; set; }

	public int ScaledX { get; set; }

	public int ScaledY { get; set; }

	public int ScaledWidth { get; set; }

	public int ScaledHeight { get; set; }

	public float ScaledWeight { get; set; }

	public int Area => ScaledWidth * ScaledHeight;

	public HaarRectangle(int[] values)
	{
		X = values[0];
		Y = values[1];
		Width = values[2];
		Height = values[3];
		Weight = values[4];
	}

	public HaarRectangle(int x, int y, int width, int height, float weight)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
		Weight = weight;
	}

	private HaarRectangle()
	{
	}

	public static HaarRectangle Parse(string value)
	{
		string[] array = value.Trim().Split(' ');
		int x = int.Parse(array[0], CultureInfo.InvariantCulture);
		int y = int.Parse(array[1], CultureInfo.InvariantCulture);
		int width = int.Parse(array[2], CultureInfo.InvariantCulture);
		int height = int.Parse(array[3], CultureInfo.InvariantCulture);
		float weight = float.Parse(array[4], CultureInfo.InvariantCulture);
		return new HaarRectangle(x, y, width, height, weight);
	}

	public void ScaleRectangle(float scale)
	{
		ScaledX = (int)((float)X * scale);
		ScaledY = (int)((float)Y * scale);
		ScaledWidth = (int)((float)Width * scale);
		ScaledHeight = (int)((float)Height * scale);
	}

	public void ScaleWeight(float scale)
	{
		ScaledWeight = Weight * scale;
	}

	public object Clone()
	{
		return new HaarRectangle
		{
			Height = Height,
			ScaledHeight = ScaledHeight,
			ScaledWeight = ScaledWeight,
			ScaledWidth = ScaledWidth,
			ScaledX = ScaledX,
			ScaledY = ScaledY,
			Weight = Weight,
			Width = Width,
			X = X,
			Y = Y
		};
	}
}
