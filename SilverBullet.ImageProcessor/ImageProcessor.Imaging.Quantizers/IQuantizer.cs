using System.Drawing;

namespace ImageProcessor.Imaging.Quantizers;

public interface IQuantizer
{
	Bitmap Quantize(Image source);
}
