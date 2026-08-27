using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Filters.Photo;

internal class PolaroidMatrixFilter : MatrixFilterBase
{
	public override ColorMatrix Matrix => ColorMatrixes.Polaroid;

	public override Bitmap TransformImage(Image source, Image destination)
	{
		using (Graphics graphics = Graphics.FromImage(destination))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(Matrix);
			Rectangle destRect = new Rectangle(0, 0, source.Width, source.Height);
			graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		destination = Adjustments.Contrast(destination, -25, null);
		destination = Effects.Glow(destination, Color.FromArgb(70, 255, 153, 102), null);
		destination = Effects.Vignette(destination, Color.FromArgb(220, 102, 34, 0), null);
		return (Bitmap)destination;
	}
}
