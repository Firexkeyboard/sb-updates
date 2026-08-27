using OpenCvSharp;

namespace OpenBullet.ImageProcessor.Layers;

public class CvtColorLayer
{
	public ColorConversionCodes Code { get; set; }

	public int DstCn { get; set; }

	public CvtColorLayer(ColorConversionCodes code, int dstCn)
	{
		Code = code;
		DstCn = dstCn;
	}
}
