using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Processors;

public class Transparency : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5] { 1f, 0f, 0f, 0f, 0f },
			new float[5] { 0f, 1f, 0f, 0f, 0f },
			new float[5] { 0f, 0f, 1f, 0f, 0f },
			new float[5] { 0f, 0f, 0f, 0.3f, 0f },
			new float[5] { 0f, 0f, 0f, 0f, 1f }
		});
		return CMatrix.Apply(factory, colorMatrix);
	}
}
