namespace ImageProcessor.Imaging;

public class ConvMatrix
{
	public int TopLeft;

	public int TopMid;

	public int TopRight;

	public int MidLeft;

	public int Pixel = 1;

	public int MidRight;

	public int BottomLeft;

	public int BottomMid;

	public int BottomRight;

	public int Factor = 1;

	public int Offset;

	public void SetAll(int nVal)
	{
		TopLeft = (TopMid = (TopRight = (MidLeft = (Pixel = (MidRight = (BottomLeft = (BottomMid = (BottomRight = nVal))))))));
	}
}
