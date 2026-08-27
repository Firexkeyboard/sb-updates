using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Filters.Photo;

internal class GothamMatrixFilter : MatrixFilterBase
{
	public override ColorMatrix Matrix => ColorMatrixes.GreyScale;

	public override Bitmap TransformImage(Image source, Image destination)
	{
		using (Graphics graphics = Graphics.FromImage(destination))
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(Matrix);
			Rectangle rectangle = new Rectangle(0, 0, source.Width, source.Height);
			graphics.DrawImage(source, rectangle, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
			using GraphicsPath graphicsPath = new GraphicsPath();
			graphicsPath.AddRectangle(rectangle);
			using SolidBrush solidBrush = new SolidBrush(Color.FromArgb(77, 38, 14, 28));
			Region clip = graphics.Clip;
			graphics.Clip = new Region(rectangle);
			graphics.FillRectangle(solidBrush, rectangle);
			solidBrush.Color = Color.FromArgb(51, 29, 32, 59);
			graphics.FillRectangle(solidBrush, rectangle);
			graphics.Clip = clip;
		}
		destination = Adjustments.Brightness(destination, 5, null);
		destination = Adjustments.Contrast(destination, 85, null);
		return (Bitmap)destination;
	}
}
