using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascade_
{
	private byte maxWeakCountField;

	private double stageThresholdField;

	private opencv_storageCascade__[] weakClassifiersField;

	public byte maxWeakCount
	{
		get
		{
			return maxWeakCountField;
		}
		set
		{
			maxWeakCountField = value;
		}
	}

	public double stageThreshold
	{
		get
		{
			return stageThresholdField;
		}
		set
		{
			stageThresholdField = value;
		}
	}

	[XmlArrayItem("_", IsNullable = false)]
	public opencv_storageCascade__[] weakClassifiers
	{
		get
		{
			return weakClassifiersField;
		}
		set
		{
			weakClassifiersField = value;
		}
	}
}
