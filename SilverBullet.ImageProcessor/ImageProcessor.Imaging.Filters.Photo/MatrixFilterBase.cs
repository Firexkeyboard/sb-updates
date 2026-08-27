using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Imaging.Filters.Photo;

public abstract class MatrixFilterBase : IMatrixFilter, IEquatable<IMatrixFilter>
{
	public abstract ColorMatrix Matrix { get; }

	public abstract Bitmap TransformImage(Image source, Image destination);

	public override bool Equals(object obj)
	{
		if (obj is IMatrixFilter other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(IMatrixFilter other)
	{
		if (other != null && GetType() == other.GetType())
		{
			if (Matrix != null && other.Matrix != null)
			{
				if (Matrix.Matrix00 == other.Matrix.Matrix00 && Matrix.Matrix01 == other.Matrix.Matrix01 && Matrix.Matrix02 == other.Matrix.Matrix02 && Matrix.Matrix03 == other.Matrix.Matrix03 && Matrix.Matrix04 == other.Matrix.Matrix04 && Matrix.Matrix10 == other.Matrix.Matrix10 && Matrix.Matrix11 == other.Matrix.Matrix11 && Matrix.Matrix12 == other.Matrix.Matrix12 && Matrix.Matrix13 == other.Matrix.Matrix13 && Matrix.Matrix14 == other.Matrix.Matrix14 && Matrix.Matrix20 == other.Matrix.Matrix20 && Matrix.Matrix21 == other.Matrix.Matrix21 && Matrix.Matrix22 == other.Matrix.Matrix22 && Matrix.Matrix23 == other.Matrix.Matrix23 && Matrix.Matrix24 == other.Matrix.Matrix24 && Matrix.Matrix30 == other.Matrix.Matrix30 && Matrix.Matrix31 == other.Matrix.Matrix31 && Matrix.Matrix32 == other.Matrix.Matrix32 && Matrix.Matrix33 == other.Matrix.Matrix33 && Matrix.Matrix34 == other.Matrix.Matrix34 && Matrix.Matrix40 == other.Matrix.Matrix40 && Matrix.Matrix41 == other.Matrix.Matrix41 && Matrix.Matrix42 == other.Matrix.Matrix42 && Matrix.Matrix43 == other.Matrix.Matrix43)
				{
					return Matrix.Matrix44 == other.Matrix.Matrix44;
				}
				return false;
			}
			return Matrix == other.Matrix;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (GetType(), Matrix?.Matrix00, Matrix?.Matrix01, Matrix?.Matrix02, Matrix?.Matrix03, Matrix?.Matrix04, Matrix?.Matrix10, Matrix?.Matrix11, Matrix?.Matrix12, Matrix?.Matrix13, Matrix?.Matrix14, Matrix?.Matrix20, Matrix?.Matrix21, Matrix?.Matrix22, Matrix?.Matrix23, Matrix?.Matrix24, Matrix?.Matrix30, Matrix?.Matrix31, Matrix?.Matrix32, Matrix?.Matrix33, Matrix?.Matrix34, Matrix?.Matrix40, Matrix?.Matrix41, Matrix?.Matrix42, Matrix?.Matrix43, Matrix?.Matrix44).GetHashCode();
	}
}
