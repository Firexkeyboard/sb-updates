using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[XmlType(AnonymousType = true)]
public class opencv_storageCascade
{
	private string stageTypeField;

	private string featureTypeField;

	private byte heightField;

	private byte widthField;

	private opencv_storageCascadeStageParams stageParamsField;

	private opencv_storageCascadeFeatureParams featureParamsField;

	private byte stageNumField;

	private opencv_storageCascade_[] stagesField;

	private opencv_storageCascade_2[] featuresField;

	private string type_idField;

	public string stageType
	{
		get
		{
			return stageTypeField;
		}
		set
		{
			stageTypeField = value;
		}
	}

	public string featureType
	{
		get
		{
			return featureTypeField;
		}
		set
		{
			featureTypeField = value;
		}
	}

	public byte height
	{
		get
		{
			return heightField;
		}
		set
		{
			heightField = value;
		}
	}

	public byte width
	{
		get
		{
			return widthField;
		}
		set
		{
			widthField = value;
		}
	}

	public opencv_storageCascadeStageParams stageParams
	{
		get
		{
			return stageParamsField;
		}
		set
		{
			stageParamsField = value;
		}
	}

	public opencv_storageCascadeFeatureParams featureParams
	{
		get
		{
			return featureParamsField;
		}
		set
		{
			featureParamsField = value;
		}
	}

	public byte stageNum
	{
		get
		{
			return stageNumField;
		}
		set
		{
			stageNumField = value;
		}
	}

	[XmlArrayItem("_", IsNullable = false)]
	public opencv_storageCascade_[] stages
	{
		get
		{
			return stagesField;
		}
		set
		{
			stagesField = value;
		}
	}

	[XmlArrayItem("_", IsNullable = false)]
	public opencv_storageCascade_2[] features
	{
		get
		{
			return featuresField;
		}
		set
		{
			featuresField = value;
		}
	}

	[XmlAttribute]
	public string type_id
	{
		get
		{
			return type_idField;
		}
		set
		{
			type_idField = value;
		}
	}
}
