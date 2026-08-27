using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

[Serializable]
public sealed class HaarFeature : IXmlSerializable, ICloneable
{
	public bool Tilted { get; set; }

	public HaarRectangle[] Rectangles { get; set; }

	public HaarFeature()
	{
		Rectangles = new HaarRectangle[2];
	}

	public HaarFeature(params HaarRectangle[] rectangles)
	{
		Rectangles = rectangles;
	}

	public HaarFeature(params int[][] rectangles)
		: this(tilted: false, rectangles)
	{
	}

	public HaarFeature(bool tilted, params int[][] rectangles)
	{
		Tilted = tilted;
		Rectangles = new HaarRectangle[rectangles.Length];
		for (int i = 0; i < rectangles.Length; i++)
		{
			Rectangles[i] = new HaarRectangle(rectangles[i]);
		}
	}

	public double GetSum(FastBitmap image, int x, int y)
	{
		double num = 0.0;
		if (!Tilted)
		{
			HaarRectangle[] rectangles = Rectangles;
			foreach (HaarRectangle haarRectangle in rectangles)
			{
				num += (double)((float)image.GetSum(x + haarRectangle.ScaledX, y + haarRectangle.ScaledY, haarRectangle.ScaledWidth, haarRectangle.ScaledHeight) * haarRectangle.ScaledWeight);
			}
		}
		else
		{
			HaarRectangle[] rectangles = Rectangles;
			foreach (HaarRectangle haarRectangle2 in rectangles)
			{
				num += (double)((float)image.GetSumT(x + haarRectangle2.ScaledX, y + haarRectangle2.ScaledY, haarRectangle2.ScaledWidth, haarRectangle2.ScaledHeight) * haarRectangle2.ScaledWeight);
			}
		}
		return num;
	}

	public void SetScaleAndWeight(float scale, float weight)
	{
		if (Rectangles.Length == 2)
		{
			HaarRectangle haarRectangle = Rectangles[0];
			HaarRectangle haarRectangle2 = Rectangles[1];
			haarRectangle2.ScaleRectangle(scale);
			haarRectangle2.ScaleWeight(weight);
			haarRectangle.ScaleRectangle(scale);
			haarRectangle.ScaledWeight = (0f - (float)haarRectangle2.Area * haarRectangle2.ScaledWeight) / (float)haarRectangle.Area;
		}
		else
		{
			HaarRectangle haarRectangle3 = Rectangles[0];
			HaarRectangle haarRectangle4 = Rectangles[1];
			HaarRectangle haarRectangle5 = Rectangles[2];
			haarRectangle5.ScaleRectangle(scale);
			haarRectangle5.ScaleWeight(weight);
			haarRectangle4.ScaleRectangle(scale);
			haarRectangle4.ScaleWeight(weight);
			haarRectangle3.ScaleRectangle(scale);
			haarRectangle3.ScaledWeight = (0f - ((float)haarRectangle4.Area * haarRectangle4.ScaledWeight + (float)haarRectangle5.Area * haarRectangle5.ScaledWeight)) / (float)haarRectangle3.Area;
		}
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		throw new NotSupportedException();
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		reader.ReadStartElement("feature");
		reader.ReadToFollowing("rects");
		reader.ReadToFollowing("_");
		List<HaarRectangle> list = new List<HaarRectangle>();
		while (reader.Name == "_")
		{
			string value = reader.ReadElementContentAsString();
			list.Add(HaarRectangle.Parse(value));
			while (reader.Name != "_" && reader.Name != "tilted" && reader.NodeType != XmlNodeType.EndElement)
			{
				reader.Read();
			}
		}
		Rectangles = list.ToArray();
		reader.ReadToFollowing("tilted", reader.BaseURI);
		Tilted = reader.ReadElementContentAsInt() == 1;
		reader.ReadEndElement();
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		throw new NotSupportedException();
	}

	public object Clone()
	{
		HaarRectangle[] array = new HaarRectangle[Rectangles.Length];
		for (int i = 0; i < array.Length; i++)
		{
			HaarRectangle haarRectangle = Rectangles[i];
			array[i] = new HaarRectangle(haarRectangle.X, haarRectangle.Y, haarRectangle.Width, haarRectangle.Height, haarRectangle.Weight);
		}
		return new HaarFeature
		{
			Rectangles = array,
			Tilted = Tilted
		};
	}
}
