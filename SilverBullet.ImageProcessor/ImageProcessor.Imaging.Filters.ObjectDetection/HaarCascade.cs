using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
public class HaarCascade : ICloneable
{
	public int Width { get; protected set; }

	public int Height { get; protected set; }

	public HaarCascadeStage[] Stages { get; protected set; }

	public bool HasTiltedFeatures { get; protected set; }

	public HaarCascade(int baseWidth, int baseHeight, HaarCascadeStage[] stages)
	{
		Width = baseWidth;
		Height = baseHeight;
		Stages = stages;
		HasTiltedFeatures = checkTiltedFeatures(stages);
	}

	protected HaarCascade(int baseWidth, int baseHeight)
	{
		Width = baseWidth;
		Height = baseHeight;
	}

	private static bool checkTiltedFeatures(HaarCascadeStage[] stages)
	{
		for (int i = 0; i < stages.Length; i++)
		{
			HaarFeatureNode[][] trees = stages[i].Trees;
			foreach (HaarFeatureNode[] array in trees)
			{
				for (int k = 0; k < array.Length; k++)
				{
					if (array[k].Feature.Tilted)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public object Clone()
	{
		HaarCascadeStage[] array = new HaarCascadeStage[Stages.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (HaarCascadeStage)Stages[i].Clone();
		}
		return new HaarCascade(Width, Height)
		{
			HasTiltedFeatures = HasTiltedFeatures,
			Stages = array
		};
	}

	public static HaarCascade FromXml(Stream stream)
	{
		return FromXml(new StreamReader(stream));
	}

	public static HaarCascade FromXml(string path)
	{
		return FromXml(new StreamReader(path));
	}

	public static HaarCascade FromXml(TextReader stringReader)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
		xmlTextReader.ReadToFollowing("size");
		string text = xmlTextReader.ReadElementContentAsString();
		xmlTextReader.ReadToFollowing("stages");
		HaarCascadeSerializationObject haarCascadeSerializationObject = (HaarCascadeSerializationObject)new XmlSerializer(typeof(HaarCascadeSerializationObject)).Deserialize(xmlTextReader);
		string[] array = text.Trim().Split(' ');
		int baseWidth = int.Parse(array[0], CultureInfo.InvariantCulture);
		int baseHeight = int.Parse(array[1], CultureInfo.InvariantCulture);
		return new HaarCascade(baseWidth, baseHeight, haarCascadeSerializationObject.Stages);
	}

	public void ToCode(string path, string className)
	{
		ToCode(new StreamWriter(path), className);
	}

	public void ToCode(TextWriter textWriter, string className)
	{
		new HaarCascadeWriter(textWriter).Write(this, className);
	}
}
