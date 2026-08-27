using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascade_2
{
	private string[] rectsField;

	[XmlArrayItem("_", IsNullable = false)]
	public string[] rects
	{
		get
		{
			return rectsField;
		}
		set
		{
			rectsField = value;
		}
	}
}
