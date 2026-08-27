using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Imaging.Filters.Photo;

public interface IMatrixFilter
{
	ColorMatrix Matrix { get; }

	Bitmap TransformImage(Image source, Image destination);
}
