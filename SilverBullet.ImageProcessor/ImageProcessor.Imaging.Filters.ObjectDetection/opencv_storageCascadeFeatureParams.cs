using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascadeFeatureParams
{
	private byte maxCatCountField;

	public byte maxCatCount
	{
		get
		{
			return maxCatCountField;
		}
		set
		{
			maxCatCountField = value;
		}
	}
}
