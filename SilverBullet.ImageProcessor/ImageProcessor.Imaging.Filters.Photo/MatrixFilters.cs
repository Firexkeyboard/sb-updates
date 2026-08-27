namespace ImageProcessor.Imaging.Filters.Photo;

public static class MatrixFilters
{
	public static IMatrixFilter BlackWhite => new BlackWhiteMatrixFilter();

	public static IMatrixFilter Comic => new ComicMatrixFilter();

	public static IMatrixFilter Gotham => new GothamMatrixFilter();

	public static IMatrixFilter GreyScale => new GreyScaleMatrixFilter();

	public static IMatrixFilter HiSatch => new HiSatchMatrixFilter();

	public static IMatrixFilter Invert => new InvertMatrixFilter();

	public static IMatrixFilter Lomograph => new LomographMatrixFilter();

	public static IMatrixFilter LoSatch => new LoSatchMatrixFilter();

	public static IMatrixFilter Polaroid => new PolaroidMatrixFilter();

	public static IMatrixFilter Sepia => new SepiaMatrixFilter();
}
