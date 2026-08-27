using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Imaging.Filters.Photo;

internal class InvertMatrixFilter : MatrixFilterBase
{
	public override ColorMatrix Matrix => ColorMatrixes.Invert;

	public override Bitmap TransformImage(Image source, Image destination)
	{
		using (Graphics graphics = Graphics.FromImage(destination))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(Matrix);
			Rectangle destRect = new Rectangle(0, 0, source.Width, source.Height);
			graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		return (Bitmap)destination;
	}
}
