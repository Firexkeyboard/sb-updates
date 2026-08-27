using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using OpenBullet.Views.CustomMessageBox;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.ViewModels;

namespace OpenBullet;

public class LoggerViewModel : ViewModelBase, ILogger
{
	private ObservableCollection<LogEntry> _entriesCollection;

	private bool onlyErrors;

	private string searchString = "";

	public ObservableCollection<LogEntry> EntriesCollection
	{
		get
		{
			return _entriesCollection;
		}
		set
		{
			if (!object.Equals(_entriesCollection, value))
			{
				_entriesCollection = value;
				OnPropertyChanged("Entries");
				OnPropertyChanged("EntriesCollection");
			}
		}
	}

	public IEnumerable<LogEntry> Entries => EntriesCollection;

	public bool Enabled
	{
		get
		{
			try
			{
				return SB.SBSettings.General.EnableLogging;
			}
			catch
			{
				return false;
			}
		}
	}

	public int BufferSize
	{
		get
		{
			try
			{
				return SB.SBSettings.General.LogBufferSize;
			}
			catch
			{
				return 0;
			}
		}
	}

	public bool OnlyErrors
	{
		get
		{
			return onlyErrors;
		}
		set
		{
			if (onlyErrors != value)
			{
				onlyErrors = value;
				OnPropertyChanged("OnlyErrors");
				Refresh();
			}
		}
	}

	public string SearchString
	{
		get
		{
			return searchString;
		}
		set
		{
			if (!string.Equals(searchString, value, StringComparison.Ordinal))
			{
				searchString = value;
				OnPropertyChanged("SearchString");
			}
		}
	}

	public LoggerViewModel()
	{
		EntriesCollection = new ObservableCollection<LogEntry>();
		((CollectionView)CollectionViewSource.GetDefaultView(EntriesCollection)).Filter = ErrorFilter;
	}

	public void Refresh()
	{
		try
		{
			CollectionViewSource.GetDefaultView(EntriesCollection).Refresh();
		}
		catch
		{
		}
	}

	private bool ErrorFilter(object item)
	{
		if (SearchString != string.Empty && !((LogEntry)((item is LogEntry) ? item : null)).LogString.ToLower().Contains(SearchString.ToLower()))
		{
			return false;
		}
		if (!SB.Logger.OnlyErrors)
		{
			return true;
		}
		return (int)((LogEntry)((item is LogEntry) ? item : null)).LogLevel == 2;
	}

	public void Log(string message, LogLevel level, bool prompt = false, int timeout = 0)
	{
		Log(Components.Unknown, level, message, prompt, timeout);
	}

	public MessageBoxResult Log(Components component, LogLevel level, string message, bool prompt = false, int timeout = 0, bool isCancelButtonVisible = false)
	{
		if (prompt)
		{
			if (timeout == 0)
			{
				if (!isCancelButtonVisible)
				{
					CustomMsgBox.Show(message, level.ToString());
					return MessageBoxResult.OK;
				}
				return CustomMsgBox.ShowWithCancel(message, level.ToString());
			}
			Form w = new Form
			{
				Size = new System.Drawing.Size(0, 0)
			};
			Task.Delay(TimeSpan.FromSeconds(timeout)).ContinueWith(delegate
			{
				w.Close();
			}, TaskScheduler.FromCurrentSynchronizationContext());
			System.Windows.Forms.MessageBox.Show(w, message, level.ToString());
		}
		if (!Enabled)
		{
			return MessageBoxResult.None;
		}
		System.Windows.Application.Current.Dispatcher.Invoke(delegate
		{
			LogEntry entry = new LogEntry(component.ToString(), message, level);
			InsertEntry(entry);
			LogToFile(entry);
		});
		return MessageBoxResult.None;
	}

	public void LogInfo(Components component, string message, bool prompt = false, int timeout = 0)
	{
		Log(component, (LogLevel)0, message, prompt, timeout);
	}

	public void LogWarning(Components component, string message, bool prompt = false, int timeout = 0)
	{
		Log(component, (LogLevel)1, message, prompt, timeout);
	}

	public void LogError(Components component, string message, bool prompt = false, int timeout = 0)
	{
		Log(component, (LogLevel)2, message, prompt, timeout);
	}

	private void InsertEntry(LogEntry entry)
	{
		try
		{
			EntriesCollection.Insert(0, entry);
			int count = EntriesCollection.Count;
			if (count > BufferSize)
			{
				EntriesCollection.RemoveAt(count - 1);
			}
		}
		catch
		{
		}
	}

	private static void LogToFile(LogEntry entry)
	{
		try
		{
			if (SB.SBSettings.General.LogToFile)
			{
				File.AppendAllText(SB.logFile, $"[{entry.LogTime}] ({entry.LogLevel}) {entry.LogComponent} - " + entry.LogString + Environment.NewLine);
			}
		}
		catch
		{
		}
	}
}
