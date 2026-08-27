using System;

namespace ImageProcessor.Imaging;

public class ComputerArchitectureInfo : IComputerArchitectureInfo
{
	public bool IsLittleEndian()
	{
		return BitConverter.IsLittleEndian;
	}
}
