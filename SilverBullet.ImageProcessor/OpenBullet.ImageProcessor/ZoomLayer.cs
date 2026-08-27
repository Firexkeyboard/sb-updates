namespace OpenBullet.ImageProcessor;

public class ZoomLayer
{
	public int ZoomFactor { get; private set; }

	public bool NearestNeighbor { get; private set; }

	public ZoomLayer(int zoomFactor, bool nearestNeighbor)
	{
		ZoomFactor = zoomFactor;
		NearestNeighbor = nearestNeighbor;
	}
}
