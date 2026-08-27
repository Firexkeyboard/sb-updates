using System;
using System.Drawing;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
public class HaarClassifier
{
	private HaarCascade cascade;

	private float invArea;

	private float scale;

	public HaarCascade Cascade => cascade;

	public float Scale
	{
		get
		{
			return scale;
		}
		set
		{
			if (scale == value)
			{
				return;
			}
			scale = value;
			invArea = 1f / ((float)(cascade.Width * cascade.Height) * scale * scale);
			HaarCascadeStage[] stages = cascade.Stages;
			for (int i = 0; i < stages.Length; i++)
			{
				HaarFeatureNode[][] trees = stages[i].Trees;
				foreach (HaarFeatureNode[] array in trees)
				{
					for (int k = 0; k < array.Length; k++)
					{
						array[k].Feature.SetScaleAndWeight(value, invArea);
					}
				}
			}
		}
	}

	public HaarClassifier(HaarCascade cascade)
	{
		this.cascade = cascade;
	}

	public HaarClassifier(int baseWidth, int baseHeight, HaarCascadeStage[] stages)
		: this(new HaarCascade(baseWidth, baseHeight, stages))
	{
	}

	public bool Compute(FastBitmap image, Rectangle rectangle)
	{
		int x = rectangle.X;
		int y = rectangle.Y;
		int width = rectangle.Width;
		int height = rectangle.Height;
		double num = (float)image.GetSum(x, y, width, height) * invArea;
		double num2 = (double)((float)image.GetSum2(x, y, width, height) * invArea) - num * num;
		double factor = ((num2 >= 0.0) ? Math.Sqrt(num2) : 1.0);
		HaarCascadeStage[] stages = cascade.Stages;
		for (int i = 0; i < stages.Length; i++)
		{
			if (!stages[i].Classify(image, x, y, factor))
			{
				return false;
			}
		}
		return true;
	}
}
