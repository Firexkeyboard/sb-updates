namespace OpenBullet.ImageProcessor.Layers;

public class FastNlMeansDenoisingColoredLayer
{
	public float Strength { get; private set; }

	public float ColorStrength { get; private set; }

	public FastNlMeansDenoisingColoredLayer(float strength, float colorStrength)
	{
		Strength = strength;
		ColorStrength = colorStrength;
	}
}
