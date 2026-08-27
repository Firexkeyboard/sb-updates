using System;
using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
public class HaarFeatureNode : ICloneable
{
	private int rightNodeIndex = -1;

	private int leftNodeIndex = -1;

	[XmlElement("threshold")]
	public double Threshold { get; set; }

	[XmlElement("left_val")]
	public double LeftValue { get; set; }

	[XmlElement("right_val")]
	public double RightValue { get; set; }

	[XmlElement("left_node")]
	public int LeftNodeIndex
	{
		get
		{
			return leftNodeIndex;
		}
		set
		{
			leftNodeIndex = value;
		}
	}

	[XmlElement("right_node")]
	public int RightNodeIndex
	{
		get
		{
			return rightNodeIndex;
		}
		set
		{
			rightNodeIndex = value;
		}
	}

	[XmlElement("feature", IsNullable = false)]
	public HaarFeature Feature { get; set; }

	public HaarFeatureNode()
	{
	}

	public HaarFeatureNode(double threshold, double leftValue, double rightValue, params int[][] rectangles)
		: this(threshold, leftValue, rightValue, tilted: false, rectangles)
	{
	}

	public HaarFeatureNode(double threshold, double leftValue, double rightValue, bool tilted, params int[][] rectangles)
	{
		Feature = new HaarFeature(tilted, rectangles);
		Threshold = threshold;
		LeftValue = leftValue;
		RightValue = rightValue;
	}

	public object Clone()
	{
		return new HaarFeatureNode
		{
			Feature = (HaarFeature)Feature.Clone(),
			Threshold = Threshold,
			RightValue = RightValue,
			LeftValue = LeftValue,
			LeftNodeIndex = leftNodeIndex,
			RightNodeIndex = rightNodeIndex
		};
	}
}
