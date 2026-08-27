using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Processors;

public class SepiaTone : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
		{
			new float[5] { 0.393f, 0.349f, 0.272f, 0f, 0f },
			new float[5] { 0.769f, 0.686f, 0.534f, 0f, 0f },
			new float[5] { 0.189f, 0.168f, 0.131f, 0f, 0f },
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5] { 0f, 0f, 0f, 0f, 1f }
		});
		return CMatrix.Apply(factory, colorMatrix);
	}
}
