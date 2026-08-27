using System.Drawing;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

public interface IWuQuantizer : IQuantizer
{
	Bitmap Quantize(Image image, int alphaThreshold, int alphaFader);
}
