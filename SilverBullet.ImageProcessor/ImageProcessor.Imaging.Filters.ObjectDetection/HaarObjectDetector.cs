using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

public class HaarObjectDetector
{
	private List<Rectangle> detectedObjects;

	private HaarClassifier classifier;

	private ObjectDetectorSearchMode searchMode = ObjectDetectorSearchMode.NoOverlap;

	private ObjectDetectorScalingMode scalingMode;

	private Size minSize = new Size(15, 15);

	private Size maxSize = new Size(500, 500);

	private float factor = 1.2f;

	private Rectangle[] lastObjects;

	private int steadyThreshold = 2;

	private int baseWidth;

	private int baseHeight;

	private int lastWidth;

	private int lastHeight;

	private float[] steps;

	private RectangleGroupMatching match;

	public Size MinSize
	{
		get
		{
			return minSize;
		}
		set
		{
			minSize = value;
		}
	}

	public Size MaxSize
	{
		get
		{
			return maxSize;
		}
		set
		{
			maxSize = value;
		}
	}

	public float ScalingFactor
	{
		get
		{
			return factor;
		}
		set
		{
			if (value != factor)
			{
				factor = value;
				steps = null;
			}
		}
	}

	public ObjectDetectorSearchMode SearchMode
	{
		get
		{
			return searchMode;
		}
		set
		{
			searchMode = value;
		}
	}

	public ObjectDetectorScalingMode ScalingMode
	{
		get
		{
			return scalingMode;
		}
		set
		{
			if (value != scalingMode)
			{
				scalingMode = value;
				steps = null;
			}
		}
	}

	public int Suppression
	{
		get
		{
			return match.MinimumNeighbors;
		}
		set
		{
			match.MinimumNeighbors = value;
		}
	}

	public Rectangle[] DetectedObjects => detectedObjects.ToArray();

	public HaarClassifier Classifier => classifier;

	public int Steady { get; private set; }

	public HaarObjectDetector(HaarCascade cascade)
		: this(cascade, 15)
	{
	}

	public HaarObjectDetector(HaarCascade cascade, int minSize)
		: this(cascade, minSize, ObjectDetectorSearchMode.NoOverlap)
	{
	}

	public HaarObjectDetector(HaarCascade cascade, int minSize, ObjectDetectorSearchMode searchMode)
		: this(cascade, minSize, searchMode, 1.2f)
	{
	}

	public HaarObjectDetector(HaarCascade cascade, int minSize, ObjectDetectorSearchMode searchMode, float scaleFactor)
		: this(cascade, minSize, searchMode, scaleFactor, ObjectDetectorScalingMode.SmallerToGreater)
	{
	}

	public HaarObjectDetector(HaarCascade cascade, int minSize, ObjectDetectorSearchMode searchMode, float scaleFactor, ObjectDetectorScalingMode scalingMode)
	{
		classifier = new HaarClassifier(cascade);
		this.minSize = new Size(minSize, minSize);
		this.searchMode = searchMode;
		ScalingMode = scalingMode;
		factor = scaleFactor;
		detectedObjects = new List<Rectangle>();
		baseWidth = cascade.Width;
		baseHeight = cascade.Height;
		match = new RectangleGroupMatching(0);
	}

	public Rectangle[] ProcessFrame(Bitmap image)
	{
		FastBitmap fastBitmap = new FastBitmap(image, classifier.Cascade.HasTiltedFeatures);
		try
		{
			detectedObjects.Clear();
			int width = fastBitmap.Width;
			int height = fastBitmap.Height;
			if (steps == null || width != lastWidth || height != lastHeight)
			{
				update(width, height);
			}
			Rectangle result = Rectangle.Empty;
			for (int i = 0; i < steps.Length; i++)
			{
				float num = steps[i];
				classifier.Scale = num;
				result.Width = (int)((float)baseWidth * num);
				result.Height = (int)((float)baseHeight * num);
				if (result.Width < minSize.Width || result.Height < minSize.Height)
				{
					if (scalingMode == ObjectDetectorScalingMode.GreaterToSmaller)
					{
						break;
					}
					continue;
				}
				if (result.Width > maxSize.Width || result.Height > maxSize.Height)
				{
					if (scalingMode != 0)
					{
						break;
					}
					continue;
				}
				int xStep = result.Width >> 3;
				int yStep = result.Height >> 3;
				int xEnd = width - result.Width;
				int num2 = height - result.Height;
				ConcurrentBag<Rectangle> bag = new ConcurrentBag<Rectangle>();
				int toExclusive = (int)Math.Ceiling((double)num2 / (double)yStep);
				Rectangle window1 = result;
				Parallel.For(0, toExclusive, delegate(int j, ParallelLoopState options)
				{
					int y = j * yStep;
					Rectangle rectangle = window1;
					rectangle.Y = y;
					for (int k = 0; k < xEnd; k += xStep)
					{
						if (options.ShouldExitCurrentIteration)
						{
							break;
						}
						rectangle.X = k;
						if (classifier.Compute(fastBitmap, rectangle))
						{
							bag.Add(rectangle);
							if (searchMode == ObjectDetectorSearchMode.Single)
							{
								options.Stop();
							}
						}
					}
				});
				if (searchMode == ObjectDetectorSearchMode.NoOverlap)
				{
					foreach (Rectangle item in bag)
					{
						if (!overlaps(item))
						{
							detectedObjects.Add(item);
						}
					}
					continue;
				}
				if (searchMode == ObjectDetectorSearchMode.Single)
				{
					if (bag.TryPeek(out result))
					{
						detectedObjects.Add(result);
						break;
					}
					continue;
				}
				foreach (Rectangle item2 in bag)
				{
					detectedObjects.Add(item2);
				}
			}
		}
		finally
		{
			if (fastBitmap != null)
			{
				((IDisposable)fastBitmap).Dispose();
			}
		}
		Rectangle[] array = detectedObjects.ToArray();
		if (searchMode == ObjectDetectorSearchMode.Average)
		{
			array = match.Group(array);
		}
		checkSteadiness(array);
		lastObjects = array;
		return array;
	}

	private void update(int width, int height)
	{
		List<float> list = new List<float>();
		if (scalingMode == ObjectDetectorScalingMode.SmallerToGreater)
		{
			float num = Math.Min((float)width / (float)baseWidth, (float)height / (float)baseHeight);
			float num2 = factor;
			for (float num3 = 1f; num3 < num; num3 *= num2)
			{
				list.Add(num3);
			}
		}
		else
		{
			float num4 = Math.Min((float)width / (float)baseWidth, (float)height / (float)baseHeight);
			float num5 = 1f;
			float num6 = 1f / factor;
			for (float num7 = num4; num7 > num5; num7 *= num6)
			{
				list.Add(num7);
			}
		}
		steps = list.ToArray();
		lastWidth = width;
		lastHeight = height;
	}

	private void checkSteadiness(Rectangle[] rectangles)
	{
		if (lastObjects == null || rectangles == null || rectangles.Length == 0)
		{
			Steady = 0;
			return;
		}
		bool flag = true;
		foreach (Rectangle first in rectangles)
		{
			bool flag2 = false;
			Rectangle[] array = lastObjects;
			foreach (Rectangle second in array)
			{
				if (first.IsEqual(second, steadyThreshold))
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			Steady++;
		}
		else
		{
			Steady = 0;
		}
	}

	private bool overlaps(Rectangle rect)
	{
		foreach (Rectangle detectedObject in detectedObjects)
		{
			if (rect.IntersectsWith(detectedObject))
			{
				return true;
			}
		}
		return false;
	}
}
