using System.Reflection;

namespace ImageProcessor.Imaging.Filters.ObjectDetection;

public static class EmbeddedHaarCascades
{
	private static HaarCascade frontFaceDefault;

	public static HaarCascade FrontFaceDefault => frontFaceDefault ?? (frontFaceDefault = GetCascadeFromResource("haarcascade_frontalface_legacy.xml"));

	private static HaarCascade GetCascadeFromResource(string identifier)
	{
		return HaarCascade.FromXml(Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageProcessor.Imaging.Filters.ObjectDetection.Resources." + identifier));
	}
}
