using ImageProcessor.Imaging.Quantizers;

namespace ImageProcessor.Imaging.Formats;

public interface IQuantizableImageFormat
{
	IQuantizer Quantizer { get; set; }
}
