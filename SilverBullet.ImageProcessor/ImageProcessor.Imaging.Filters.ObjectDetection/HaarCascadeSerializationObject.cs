using System;
using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
[XmlRoot(Namespace = "", IsNullable = false, ElementName = "stages")]
public class HaarCascadeSerializationObject
{
	[XmlElement("_")]
	public HaarCascadeStage[] Stages { get; set; }
}
