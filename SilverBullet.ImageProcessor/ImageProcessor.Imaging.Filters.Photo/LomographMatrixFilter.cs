using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Filters.Photo;

internal class LomographMatrixFilter : MatrixFilterBase
{
	public override ColorMatrix Matrix => ColorMatrixes.Lomograph;

	public override Bitmap TransformImage(Image source, Image destination)
	{
		using (Graphics graphics = Graphics.FromImage(destination))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(Matrix);
			Rectangle destRect = new Rectangle(0, 0, source.Width, source.Height);
			graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		destination = Effects.Vignette(destination, Color.FromArgb(220, 0, 10, 0), null);
		return (Bitmap)destination;
	}
}
