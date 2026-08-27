using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenBullet;

public class HugeFileSort
{
	private class ChunkInfo
	{
		private StreamWriter noSortWriter;

		private string noSortFileName;

		public string FileName;

		public string NoSortFileName => noSortFileName;

		public void AddSmallString(string str, Encoding encoding)
		{
			if (noSortWriter == null)
			{
				noSortFileName = GenerateFileName();
				noSortWriter = new StreamWriter(noSortFileName, append: false, encoding);
			}
			noSortWriter.WriteLine(str);
		}

		public void Close()
		{
			if (noSortWriter != null)
			{
				noSortWriter.Close();
			}
		}

		private string GenerateFileName()
		{
			return "tmp\\n" + fileCounter++ + ".txt";
		}
	}

	private class FileChunk
	{
		private StreamWriter writer;

		private long size;

		private string fileName;

		public long Size => size;

		public string FileName => fileName;

		public FileChunk(Encoding encoding)
		{
			fileName = GenerateFileName();
			writer = new StreamWriter(fileName, append: false, encoding);
		}

		private string GenerateFileName()
		{
			return "tmp\\s" + fileCounter++ + ".txt";
		}

		public void Append(string entry, Encoding encoding)
		{
			writer.WriteLine(entry);
			size += encoding.GetByteCount(entry) + encoding.GetByteCount(Environment.NewLine);
		}

		public void Close()
		{
			writer.Close();
		}
	}

	private long maxFileSize = 104857600L;

	private SortedDictionary<string, ChunkInfo> chunks;

	private static int fileCounter;

	public StringComparer Comparer { get; set; }

	public Encoding Encoding { get; set; }

	public long MaxFileSize
	{
		get
		{
			return maxFileSize;
		}
		set
		{
			maxFileSize = value;
		}
	}

	public HugeFileSort()
	{
		Comparer = StringComparer.CurrentCulture;
		Encoding = Encoding.UTF8;
	}

	public void Sort(string inputFileName, string outputFileName)
	{
		chunks = new SortedDictionary<string, ChunkInfo>(Comparer);
		if (new FileInfo(inputFileName).Length < maxFileSize)
		{
			SortFile(inputFileName, outputFileName);
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo("tmp");
		if (directoryInfo.Exists)
		{
			directoryInfo.Delete(recursive: true);
		}
		directoryInfo.Create();
		SplitFile(inputFileName, 1);
		Merge(outputFileName);
	}

	private void Merge(string outputFileName)
	{
		using FileStream output = File.Create(outputFileName);
		foreach (KeyValuePair<string, ChunkInfo> chunk in chunks)
		{
			chunk.Value.Close();
			if (chunk.Value.NoSortFileName != null)
			{
				CopyFile(chunk.Value.NoSortFileName, output);
			}
			if (chunk.Value.FileName != null)
			{
				CopyFile(chunk.Value.FileName, output);
			}
		}
	}

	private void CopyFile(string fileName, FileStream output)
	{
		using FileStream fileStream = File.OpenRead(fileName);
		fileStream.CopyTo(output);
	}

	private void SplitFile(string inputFileName, int chars)
	{
		Dictionary<string, FileChunk> dictionary = new Dictionary<string, FileChunk>(Comparer);
		using (StreamReader streamReader = new StreamReader(inputFileName, Encoding))
		{
			while (streamReader.Peek() >= 0)
			{
				string text = streamReader.ReadLine();
				if (text.Length < chars)
				{
					if (!chunks.TryGetValue(text, out var value))
					{
						chunks.Add(text, value = new ChunkInfo());
					}
					value.AddSmallString(text, Encoding);
					continue;
				}
				string key = text.Substring(0, chars);
				if (!dictionary.TryGetValue(key, out var value2))
				{
					value2 = new FileChunk(Encoding);
					dictionary.Add(key, value2);
				}
				value2.Append(text, Encoding);
			}
		}
		foreach (KeyValuePair<string, FileChunk> item in dictionary)
		{
			item.Value.Close();
			if (item.Value.Size > maxFileSize)
			{
				SplitFile(item.Value.FileName, chars + 1);
				File.Delete(item.Value.FileName);
				continue;
			}
			SortFile(item.Value.FileName, item.Value.FileName);
			if (!chunks.TryGetValue(item.Key, out var value3))
			{
				chunks.Add(item.Key, value3 = new ChunkInfo());
			}
			value3.FileName = item.Value.FileName;
		}
	}

	private void SortFile(string inputFileName, string outputFileName)
	{
		List<string> list = new List<string>((int)(new FileInfo(inputFileName).Length / 4));
		using (StreamReader streamReader = new StreamReader(inputFileName, Encoding))
		{
			while (streamReader.Peek() >= 0)
			{
				list.Add(streamReader.ReadLine());
			}
		}
		list.Sort(Comparer);
		using StreamWriter streamWriter = new StreamWriter(outputFileName, append: false, Encoding);
		foreach (string item in list)
		{
			streamWriter.WriteLine(item);
		}
	}
}
