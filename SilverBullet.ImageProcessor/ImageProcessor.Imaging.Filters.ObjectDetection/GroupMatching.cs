using System;
using System.Collections.Generic;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

public abstract class GroupMatching<T>
{
	private int classCount;

	private int minNeighbors;

	private int[] labels;

	private int[] equals;

	private List<T> filter;

	public int MinimumNeighbors
	{
		get
		{
			return minNeighbors;
		}
		set
		{
			if (minNeighbors < 0)
			{
				throw new ArgumentOutOfRangeException("value", "Value must be equal to or higher than zero.");
			}
			minNeighbors = value;
		}
	}

	public int Classes => classCount;

	protected GroupMatching(int minimumNeighbors = 2)
	{
		minNeighbors = minimumNeighbors;
		filter = new List<T>();
	}

	public T[] Group(T[] shapes)
	{
		classify(shapes);
		int[] neighborCounts;
		T[] array = Average(labels, shapes, out neighborCounts);
		if (minNeighbors > 0)
		{
			filter.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				if (neighborCounts[i] >= minNeighbors)
				{
					filter.Add(array[i]);
				}
			}
			return filter.ToArray();
		}
		return array;
	}

	private void classify(T[] shapes)
	{
		equals = new int[shapes.Length];
		for (int i = 0; i < equals.Length; i++)
		{
			equals[i] = -1;
		}
		labels = new int[shapes.Length];
		for (int j = 0; j < labels.Length; j++)
		{
			labels[j] = j;
		}
		classCount = 0;
		for (int k = 0; k < shapes.Length - 1; k++)
		{
			for (int l = k + 1; l < shapes.Length; l++)
			{
				if (Near(shapes[k], shapes[l]))
				{
					merge(labels[k], labels[l]);
				}
			}
		}
		int[] array = new int[shapes.Length];
		for (int m = 0; m < array.Length; m++)
		{
			if (equals[m] == -1)
			{
				array[m] = classCount++;
			}
		}
		for (int n = 0; n < shapes.Length; n++)
		{
			int num = labels[n];
			while (equals[num] != -1)
			{
				num = equals[num];
			}
			labels[n] = array[num];
		}
	}

	private void merge(int label1, int label2)
	{
		int num = label1;
		int num2 = label2;
		while (equals[num] != -1)
		{
			num = equals[num];
		}
		while (equals[num2] != -1)
		{
			num2 = equals[num2];
		}
		if (num == num2)
		{
			return;
		}
		int num3;
		int num4;
		int num5;
		if (num > num2)
		{
			num3 = num;
			num4 = num2;
			num5 = label1;
		}
		else
		{
			num3 = num2;
			num4 = num;
			num5 = label2;
		}
		equals[num3] = num4;
		for (int i = num3 + 1; i <= num5; i++)
		{
			if (equals[i] == num3)
			{
				equals[i] = num4;
			}
		}
	}

	protected abstract bool Near(T shape1, T shape2);

	protected abstract T[] Average(int[] labels, T[] shapes, out int[] neighborCounts);
}
