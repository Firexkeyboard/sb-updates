using System.Collections.Generic;
using System.Drawing;
using ImageProcessor.Imaging;
using SilverBullet.ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Sharpen : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		int num = (int)DynamicParameter;
		ConvMatrix convMatrix = new ConvMatrix();
		convMatrix.SetAll(0);
		convMatrix.Pixel = num;
		convMatrix.TopMid = (convMatrix.MidLeft = (convMatrix.MidRight = (convMatrix.BottomMid = -2)));
		convMatrix.Factor = num - 8;
		return ImageHelper.Conv3x3(factory.Bitmap, convMatrix);
	}
}
