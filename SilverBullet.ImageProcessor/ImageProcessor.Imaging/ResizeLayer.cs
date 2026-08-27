using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageProcessor.Imaging;

public class ResizeLayer : IEquatable<ResizeLayer>
{
	public Size Size { get; set; }

	public Size? MaxSize { get; set; }

	public List<Size> RestrictedSizes { get; set; }

	public ResizeMode ResizeMode { get; set; }

	public AnchorPosition AnchorPosition { get; set; }

	public bool Upscale { get; set; }

	[Obsolete("Use the Center property instead.")]
	public float[] CenterCoordinates
	{
		get
		{
			PointF? center = Center;
			if (center.HasValue)
			{
				PointF valueOrDefault = center.GetValueOrDefault();
				return new float[2] { valueOrDefault.Y, valueOrDefault.X };
			}
			return null;
		}
		set
		{
			if (value != null && value.Length == 2)
			{
				Center = new PointF(value[1], value[0]);
			}
			else
			{
				Center = null;
			}
		}
	}

	public PointF? Center { get; set; }

	public Point? AnchorPoint { get; set; }

	public ResizeLayer(Size size, ResizeMode resizeMode = ResizeMode.Pad, AnchorPosition anchorPosition = AnchorPosition.Center, bool upscale = true, float[] centerCoordinates = null, Size? maxSize = null, List<Size> restrictedSizes = null, Point? anchorPoint = null)
	{
		Size = size;
		Upscale = upscale;
		ResizeMode = resizeMode;
		AnchorPosition = anchorPosition;
		if (centerCoordinates != null && centerCoordinates.Length == 2)
		{
			Center = new PointF(centerCoordinates[1], centerCoordinates[0]);
		}
		MaxSize = maxSize;
		RestrictedSizes = restrictedSizes;
		AnchorPoint = anchorPoint;
	}

	public override bool Equals(object obj)
	{
		if (obj is ResizeLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ResizeLayer other)
	{
		if (other != null && Size == other.Size)
		{
			Size? maxSize = MaxSize;
			Size? maxSize2 = other.MaxSize;
			if (maxSize.HasValue == maxSize2.HasValue && (!maxSize.HasValue || maxSize.GetValueOrDefault() == maxSize2.GetValueOrDefault()) && ((RestrictedSizes == null || other.RestrictedSizes == null) ? (RestrictedSizes == other.RestrictedSizes) : RestrictedSizes.SequenceEqual(other.RestrictedSizes)) && ResizeMode == other.ResizeMode && AnchorPosition == other.AnchorPosition && Upscale == other.Upscale && Center == other.Center)
			{
				Point? anchorPoint = AnchorPoint;
				Point? anchorPoint2 = other.AnchorPoint;
				if (anchorPoint.HasValue != anchorPoint2.HasValue)
				{
					return false;
				}
				if (!anchorPoint.HasValue)
				{
					return true;
				}
				return anchorPoint.GetValueOrDefault() == anchorPoint2.GetValueOrDefault();
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Size, MaxSize, ResizeMode, AnchorPosition, Upscale, Center, AnchorPoint).GetHashCode();
	}
}
