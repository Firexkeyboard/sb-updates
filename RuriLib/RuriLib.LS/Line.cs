using System;
using System.Collections.Generic;
using System.Drawing;

namespace RuriLib.LS;

public class Line
{
	private Point p1;

	private Point p2;

	private Random rand;

	public Line(Point p1, Point p2)
	{
		this.p1 = p1;
		this.p2 = p2;
		rand = new Random();
	}

	public Point[] getPoints(int quantity)
	{
		Point[] array = new Point[quantity];
		int num = p2.Y - p1.Y;
		int num2 = p2.X - p1.X;
		double num3 = (double)(p2.Y - p1.Y) / (double)(p2.X - p1.X);
		quantity--;
		for (double num4 = 0.0; num4 < (double)quantity; num4 += 1.0)
		{
			double num5 = ((num3 == 0.0) ? 0.0 : ((double)num * (num4 / (double)quantity)));
			double a = ((num3 == 0.0) ? ((double)num2 * (num4 / (double)quantity)) : (num5 / num3));
			array[(int)num4] = new Point((int)Math.Round(a) + p1.X, (int)Math.Round(num5) + p1.Y);
		}
		array[quantity] = p2;
		return array;
	}

	public Point[] getOffsets(int quantity)
	{
		Point[] array = new Point[quantity];
		int num = p2.Y - p1.Y;
		int num2 = p2.X - p1.X;
		double num3 = (double)(p2.Y - p1.Y) / (double)(p2.X - p1.X);
		quantity--;
		for (double num4 = 0.0; num4 < (double)quantity; num4 += 1.0)
		{
			double num5 = ((num3 == 0.0) ? 0.0 : ((double)num * (num4 / (double)quantity)));
			double a = ((num3 == 0.0) ? ((double)num2 * (num4 / (double)quantity)) : (num5 / num3));
			array[(int)num4] = new Point((int)Math.Round(a), (int)Math.Round(num5));
		}
		array[quantity] = p2;
		return array;
	}

	private static double Distance(double x1, double y1, double x2, double y2)
	{
		return Math.Sqrt(Math.Pow(x2 - x1, 2.0) + Math.Pow(y2 - y1, 2.0));
	}

	private static double Hypot(double x, double y)
	{
		return Math.Sqrt(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));
	}

	public Point[] HumanWindMouse(double xs, double ys, double xe, double ye, double gravity, double wind, double targetArea)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = Math.Sqrt(2.0);
		double num6 = Math.Sqrt(3.0);
		double num7 = Math.Sqrt(5.0);
		int num8 = (int)Distance(Math.Round(xs), Math.Round(ys), Math.Round(xe), Math.Round(ye));
		uint num9 = (uint)(Environment.TickCount + 10000);
		List<Point> list = new List<Point>();
		while (Environment.TickCount <= num9)
		{
			double num10 = Hypot(xs - xe, ys - ye);
			wind = Math.Min(wind, num10);
			if (num10 < 1.0)
			{
				num10 = 1.0;
			}
			double num11 = Math.Round(Math.Round((double)num8) * 0.3) / 7.0;
			if (num11 > 25.0)
			{
				num11 = 25.0;
			}
			if (num11 < 5.0)
			{
				num11 = 5.0;
			}
			if ((double)rand.Next(6) == 1.0)
			{
				num11 = 2.0;
			}
			double num12 = ((!(num11 <= Math.Round(num10))) ? Math.Round(num10) : num11);
			if (num10 >= targetArea)
			{
				num3 = num3 / num6 + ((double)rand.Next((int)(Math.Round(wind) * 2.0 + 1.0)) - wind) / num7;
				num4 = num4 / num6 + ((double)rand.Next((int)(Math.Round(wind) * 2.0 + 1.0)) - wind) / num7;
			}
			else
			{
				num3 /= num5;
				num4 /= num5;
			}
			num += num3;
			num2 += num4;
			num += gravity * (xe - xs) / num10;
			num2 += gravity * (ye - ys) / num10;
			if (Hypot(num, num2) > num12)
			{
				double num13 = num12 / 2.0 + (double)rand.Next((int)(Math.Round(num12) / 2.0));
				double num14 = Math.Sqrt(num * num + num2 * num2);
				num = num / num14 * num13;
				num2 = num2 / num14 * num13;
			}
			int num15 = (int)Math.Round(xs);
			int num16 = (int)Math.Round(ys);
			xs += num;
			ys += num2;
			if ((double)num15 != Math.Round(xs) || (double)num16 != Math.Round(ys))
			{
				list.Add(new Point((int)Math.Round(xs), (int)Math.Round(ys)));
			}
			if (Hypot(xs - xe, ys - ye) < 1.0)
			{
				break;
			}
		}
		if (Math.Round(xe) != Math.Round(xs) || Math.Round(ye) != Math.Round(ys))
		{
			list.Add(new Point((int)Math.Round(xe), (int)Math.Round(ye)));
		}
		return list.ToArray();
	}
}
