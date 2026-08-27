using System;
using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
[XmlRoot("_")]
public class HaarCascadeStage : ICloneable
{
	[XmlArray("trees")]
	[XmlArrayItem("_")]
	[XmlArrayItem("_", NestingLevel = 1)]
	public HaarFeatureNode[][] Trees { get; set; }

	[XmlElement("stage_threshold")]
	public double Threshold { get; set; }

	[XmlElement("parent")]
	public int ParentIndex { get; set; }

	[XmlElement("next")]
	public int NextIndex { get; set; }

	public HaarCascadeStage()
	{
	}

	public HaarCascadeStage(double threshold)
	{
		Threshold = threshold;
	}

	public HaarCascadeStage(double threshold, int parentIndex, int nextIndex)
	{
		Threshold = threshold;
		ParentIndex = parentIndex;
		NextIndex = nextIndex;
	}

	public bool Classify(FastBitmap image, int x, int y, double factor)
	{
		double num = 0.0;
		HaarFeatureNode[][] trees = Trees;
		foreach (HaarFeatureNode[] array in trees)
		{
			int num2 = 0;
			do
			{
				HaarFeatureNode haarFeatureNode = array[num2];
				if (haarFeatureNode.Feature.GetSum(image, x, y) < haarFeatureNode.Threshold * factor)
				{
					num += haarFeatureNode.LeftValue;
					num2 = haarFeatureNode.LeftNodeIndex;
				}
				else
				{
					num += haarFeatureNode.RightValue;
					num2 = haarFeatureNode.RightNodeIndex;
				}
			}
			while (num2 > 0);
		}
		if (num < Threshold)
		{
			return false;
		}
		return true;
	}

	public object Clone()
	{
		HaarFeatureNode[][] array = new HaarFeatureNode[Trees.Length][];
		for (int i = 0; i < array.Length; i++)
		{
			HaarFeatureNode[] array2 = Trees[i];
			HaarFeatureNode[] array3 = (array[i] = new HaarFeatureNode[array2.Length]);
			for (int j = 0; j < array3.Length; j++)
			{
				array3[j] = (HaarFeatureNode)array2[j].Clone();
			}
		}
		return new HaarCascadeStage
		{
			NextIndex = NextIndex,
			ParentIndex = ParentIndex,
			Threshold = Threshold,
			Trees = array
		};
	}
}
