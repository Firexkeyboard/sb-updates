using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascade__
{
	private string internalNodesField;

	private string leafValuesField;

	public string internalNodes
	{
		get
		{
			return internalNodesField;
		}
		set
		{
			internalNodesField = value;
		}
	}

	public string leafValues
	{
		get
		{
			return leafValuesField;
		}
		set
		{
			leafValuesField = value;
		}
	}
}
