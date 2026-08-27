using System;
using System.Drawing;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Colors;

public readonly struct HslaColor : IEquatable<HslaColor>
{
	public static readonly HslaColor Empty;

	public float H { get; }

	public float L { get; }

	public float S { get; }

	public float A { get; }

	private HslaColor(float hue, float saturation, float luminosity, float alpha)
	{
		H = Clamp(hue);
		S = Clamp(saturation);
		L = Clamp(luminosity);
		A = Clamp(alpha);
	}

	private HslaColor(Color color)
	{
		HslaColor hslaColor = color;
		H = hslaColor.H;
		S = hslaColor.S;
		L = hslaColor.L;
		A = hslaColor.A;
	}

	public static HslaColor FromHslaColor(float hue, float saturation, float luminosity)
	{
		return new HslaColor(hue, saturation, luminosity, 1f);
	}

	public static HslaColor FromHslaColor(float hue, float saturation, float luminosity, float alpha)
	{
		return new HslaColor(hue, saturation, luminosity, alpha);
	}

	public static HslaColor FromColor(Color color)
	{
		return new HslaColor(color);
	}

	public static implicit operator HslaColor(Color color)
	{
		return new HslaColor(color.GetHue() / 360f, color.GetSaturation(), color.GetBrightness(), (float)(int)color.A / 255f);
	}

	public static implicit operator HslaColor(RgbaColor rgbaColor)
	{
		return FromColor(rgbaColor);
	}

	public static implicit operator HslaColor(YCbCrColor ycbcrColor)
	{
		Color color = ycbcrColor;
		return new HslaColor(color.GetHue() / 360f, color.GetSaturation(), color.GetBrightness(), (float)(int)color.A / 255f);
	}

	public static implicit operator Color(HslaColor hslaColor)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		if ((double)Math.Abs(hslaColor.L - 0f) > 0.0001)
		{
			if ((double)Math.Abs(hslaColor.S - 0f) <= 0.0001)
			{
				num = (num2 = (num3 = hslaColor.L));
			}
			else
			{
				float temp = GetTemp2(in hslaColor);
				float temp2 = 2f * hslaColor.L - temp;
				num = GetColorComponent(temp2, temp, hslaColor.H + 1f / 3f);
				num2 = GetColorComponent(temp2, temp, hslaColor.H);
				num3 = GetColorComponent(temp2, temp, hslaColor.H - 1f / 3f);
			}
		}
		return Color.FromArgb(Convert.ToInt32(255f * hslaColor.A), Convert.ToInt32(255f * num), Convert.ToInt32(255f * num2), Convert.ToInt32(255f * num3));
	}

	public static implicit operator YCbCrColor(HslaColor hslaColor)
	{
		return YCbCrColor.FromColor(hslaColor);
	}

	public static implicit operator CmykColor(HslaColor hslaColor)
	{
		return CmykColor.FromColor(hslaColor);
	}

	public override string ToString()
	{
		if (IsEmpty())
		{
			return "HslaColor [ Empty ]";
		}
		return $"HslaColor [ H={H:#0.##}, S={S:#0.##}, L={L:#0.##}, A={A:#0.##}]";
	}

	public override bool Equals(object obj)
	{
		if (obj is HslaColor other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(HslaColor other)
	{
		if (H == other.H && S == other.S && L == other.L)
		{
			return A == other.A;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (H, S, L, A).GetHashCode();
	}

	private static float GetColorComponent(float temp1, float temp2, float temp3)
	{
		temp3 = MoveIntoRange(temp3);
		if ((double)temp3 < 1.0 / 6.0)
		{
			return temp1 + (temp2 - temp1) * 6f * temp3;
		}
		if ((double)temp3 < 0.5)
		{
			return temp2;
		}
		if ((double)temp3 < 2.0 / 3.0)
		{
			return temp1 + (temp2 - temp1) * (2f / 3f - temp3) * 6f;
		}
		return temp1;
	}

	private static float GetTemp2(in HslaColor hslColor)
	{
		if ((double)hslColor.L <= 0.5)
		{
			return hslColor.L * (1f + hslColor.S);
		}
		return hslColor.L + hslColor.S - hslColor.L * hslColor.S;
	}

	private static float MoveIntoRange(float temp3)
	{
		if ((double)temp3 < 0.0)
		{
			temp3 += 1f;
		}
		else if ((double)temp3 > 1.0)
		{
			temp3 -= 1f;
		}
		return temp3;
	}

	private static float Clamp(float value)
	{
		return ImageMaths.Clamp(value, 0f, 1f);
	}

	private bool IsEmpty()
	{
		if (Math.Abs(H - 0f) <= 0.0001f && Math.Abs(S - 0f) <= 0.0001f && Math.Abs(L - 0f) <= 0.0001f)
		{
			return Math.Abs(A - 0f) <= 0.0001f;
		}
		return false;
	}
}
