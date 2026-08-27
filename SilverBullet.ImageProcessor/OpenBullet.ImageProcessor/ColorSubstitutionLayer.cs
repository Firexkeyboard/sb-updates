using System.Drawing;

namespace OpenBullet.ImageProcessor;

public class ColorSubstitutionLayer
{
	public int Threshold { get; private set; }

	public Color SourceColor { get; private set; }

	public Color NewColor { get; private set; }

	public ColorSubstitutionLayer(int threshold, Color sourceColor, Color newColor)
	{
		Threshold = threshold;
		SourceColor = sourceColor;
		NewColor = newColor;
	}
}
