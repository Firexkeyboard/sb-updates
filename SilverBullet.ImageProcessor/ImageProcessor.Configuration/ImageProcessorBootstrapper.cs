using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Common.Helpers;
using ImageProcessor.Imaging.Formats;

namespace ImageProcessor.Configuration;

public sealed class ImageProcessorBootstrapper
{
	private static readonly Lazy<ImageProcessorBootstrapper> Lazy = new Lazy<ImageProcessorBootstrapper>(() => new ImageProcessorBootstrapper());

	public static ImageProcessorBootstrapper Instance => Lazy.Value;

	public IEnumerable<ISupportedImageFormat> SupportedImageFormats { get; private set; }

	public ILogger Logger { get; private set; }

	public NativeBinaryFactory NativeBinaryFactory { get; }

	private ImageProcessorBootstrapper()
	{
		NativeBinaryFactory = new NativeBinaryFactory();
		LoadSupportedImageFormats();
		LoadLogger();
	}

	public void AddImageFormats(params ISupportedImageFormat[] format)
	{
		((List<ISupportedImageFormat>)SupportedImageFormats).AddRange(format);
	}

	public void SetLogger(ILogger logger)
	{
		Logger = logger;
	}

	private void LoadSupportedImageFormats()
	{
		List<ISupportedImageFormat> list = new List<ISupportedImageFormat>
		{
			new BitmapFormat(),
			new GifFormat(),
			new JpegFormat(),
			new PngFormat(),
			new TiffFormat()
		};
		Type type = typeof(ISupportedImageFormat);
		if (SupportedImageFormats == null)
		{
			List<Type> source = (from t in TypeFinder.GetAssembliesWithKnownExclusions().SelectMany((Assembly a) => a.GetLoadableTypes())
				where type.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
				select t).ToList();
			list.AddRange(source.Select((Type f) => Activator.CreateInstance(f) as ISupportedImageFormat).ToList());
			SupportedImageFormats = list;
		}
	}

	private void LoadLogger()
	{
		Type type = typeof(ILogger);
		if (Logger != null)
		{
			return;
		}
		List<Type> list = (from t in TypeFinder.GetAssembliesWithKnownExclusions().SelectMany((Assembly a) => a.GetLoadableTypes())
			where type.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
			select t).ToList();
		if (list.Count > 0)
		{
			Logger = (from l in list
				where l != typeof(DefaultLogger)
				select l into f
				select Activator.CreateInstance(f) as ILogger).First();
		}
		else
		{
			Logger = new DefaultLogger();
		}
	}
}
