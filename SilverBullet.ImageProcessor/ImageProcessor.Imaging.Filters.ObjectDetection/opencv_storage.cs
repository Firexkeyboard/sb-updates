using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public class opencv_storage
{
	private opencv_storageCascade cascadeField;

	public opencv_storageCascade cascade
	{
		get
		{
			return cascadeField;
		}
		set
		{
			cascadeField = value;
		}
	}
}
