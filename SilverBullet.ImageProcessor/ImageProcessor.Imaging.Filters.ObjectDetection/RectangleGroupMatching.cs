using System;
using System.Drawing;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

public class RectangleGroupMatching : GroupMatching<Rectangle>
{
	private double threshold;

	protected double Threshold => threshold;

	public RectangleGroupMatching(int minimumNeighbors = 2, double threshold = 0.2)
		: base(minimumNeighbors)
	{
		this.threshold = threshold;
	}

	protected override bool Near(Rectangle shape1, Rectangle shape2)
	{
		if (shape1.Contains(shape2) || shape2.Contains(shape1))
		{
			return true;
		}
		int num = Math.Min(shape1.Height, shape2.Height);
		int num2 = Math.Min(shape1.Width, shape2.Width);
		double num3 = 0.5 * threshold * (double)(num + num2);
		if ((double)Math.Abs(shape1.X - shape2.X) <= num3 && (double)Math.Abs(shape1.Y - shape2.Y) <= num3 && (double)Math.Abs(shape1.Right - shape2.Right) <= num3)
		{
			return (double)Math.Abs(shape1.Bottom - shape2.Bottom) <= num3;
		}
		return false;
	}

	protected override Rectangle[] Average(int[] labels, Rectangle[] shapes, out int[] neighborCounts)
	{
		neighborCounts = new int[base.Classes];
		Rectangle[] array = new Rectangle[base.Classes];
		for (int i = 0; i < shapes.Length; i++)
		{
			int num = labels[i];
			array[num].X += shapes[i].X;
			array[num].Y += shapes[i].Y;
			array[num].Width += shapes[i].Width;
			array[num].Height += shapes[i].Height;
			neighborCounts[num]++;
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = new Rectangle((int)Math.Ceiling((float)array[j].X / (float)neighborCounts[j]), (int)Math.Ceiling((float)array[j].Y / (float)neighborCounts[j]), (int)Math.Ceiling((float)array[j].Width / (float)neighborCounts[j]), (int)Math.Ceiling((float)array[j].Height / (float)neighborCounts[j]));
		}
		return array;
	}
}
