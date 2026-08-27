using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascadeStageParams
{
	private byte maxWeakCountField;

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
}
