using System.Collections.Generic;
using System.Drawing;

namespace ImageProcessor.Processors;

public interface IGraphicsProcessor
{
	dynamic DynamicParameter { get; set; }

	Dictionary<string, string> Settings { get; set; }

	Image ProcessImage(ImageFactory factory);
}
