using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace OpenBullet.Views.Main.Tools;

public class TessDataDownloads : Page, IComponentConnector
{
	private HttpClient loadSite = new HttpClient();

	public string url = "https://github.com/tesseract-ocr/tessdata/tree/3.04.00";

	public string siteResponse;

	public string language;

	private Regex lang = new Regex("title=\"(.*)\" href=\"/tesseract-ocr/tessdata/blob/");

	private Task taskDl;

	internal StackPanel topMenu;

	internal System.Windows.Controls.ListBox LanguageList;

	internal System.Windows.Controls.ListBox DownloadList;

	internal System.Windows.Controls.Button DownloadBtn;

	internal System.Windows.Controls.TextBox LogsText;

	internal System.Windows.Controls.ProgressBar progressBar;

	private bool _contentLoaded;

	public TessDataDownloads()
	{
		InitializeComponent();
	}

	private void UpdateProgress(long received, long total)
	{
		try
		{
			int pct = total > 0 ? (int)(received * 100 / total) : 0;
			DispatcherInvoke(delegate
			{
				progressBar.Value = pct;
			});
		}
		catch
		{
		}
	}

	private void DownloadBtn_Click(object sender, RoutedEventArgs e)
	{
		LogsText.Clear();
		progressBar.Value = 0.0;
		int i = 0;
		System.Windows.Controls.TextBox logsText = LogsText;
		logsText.Text = logsText.Text + "Downloading tessdata files..." + Environment.NewLine;
		ItemCollection items = DownloadList.Items;
		try
		{
			taskDl?.Dispose();
		}
		catch
		{
		}
		taskDl = Task.Run(async delegate
		{
			foreach (string item in (IEnumerable)items)
			{
				if (!Directory.Exists(".\\tessdata"))
				{
					Directory.CreateDirectory(".\\tessdata");
				}
				i++;
				if (File.Exists(".\\tessdata\\" + item + ".traineddata"))
				{
					DispatcherInvoke(delegate
					{
						System.Windows.Controls.TextBox logsText2 = LogsText;
						logsText2.Text = logsText2.Text + "\n" + item + ".traineddata is exists!\n" + Environment.NewLine;
					});
				}
				else
				{
					DispatcherInvoke(delegate
					{
						LogsText.Text += $"{i}/{DownloadList.Items.Count} | Downloading: {item}..";
					});
					try
					{
						await DownloadLanguage(i, item.ToString());
					}
					catch (Exception ex)
					{
						Exception ex2 = ex;
						Exception ex3 = ex2;
						DispatcherInvoke(delegate
						{
							LogsText.Text += string.Format("\n[{0}] {1}", "Exception".ToUpper(), ex3);
						});
					}
				}
			}
		}).ContinueWith(delegate
		{
			DispatcherInvoke(delegate
			{
				LogsText.Text += "Your chosen languages have been downloaded ";
			});
		});
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		LogsText.Clear();
		LanguageList.Items.Clear();
		LogsText.Text = "Downloading language list...\n";
		Task.Run(() => siteResponse = loadSite.GetStringAsync(url).GetAwaiter().GetResult()).ContinueWith(delegate
		{
			foreach (Match item in lang.Matches(siteResponse))
			{
				string val = item.Groups[1].Value.Split('"').First();
				base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
				{
					LanguageList.Items.Add(Path.GetFileNameWithoutExtension(val) + " (" + Path.GetExtension(val).Split('.')[1] + ")");
				});
			}
			DispatcherInvoke(delegate
			{
				System.Windows.Controls.TextBox logsText = LogsText;
				logsText.Text = logsText.Text + "Downloading language list Finished!" + Environment.NewLine;
			});
		});
	}

	private void DispatcherInvoke(Action action)
	{
		base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
		{
			action?.Invoke();
		});
	}

	public async Task DownloadLanguage(int i, string language)
	{
		language = language.Split('(').First().Trim();
		language += ".traineddata";
		string destPath = AppDomain.CurrentDomain.BaseDirectory + "/tessdata/" + language;
		using var response = await loadSite.GetAsync(
			"https://github.com/tesseract-ocr/tessdata/raw/3.04.00/" + language,
			HttpCompletionOption.ResponseHeadersRead);
		long total = response.Content.Headers.ContentLength ?? -1;
		using var src = await response.Content.ReadAsStreamAsync();
		using var dest = System.IO.File.Create(destPath);
		byte[] buf = new byte[81920];
		long received = 0;
		int read;
		while ((read = await src.ReadAsync(buf, 0, buf.Length)) > 0)
		{
			await dest.WriteAsync(buf, 0, read);
			received += read;
			UpdateProgress(received, total);
		}
		DispatcherInvoke(delegate
		{
			System.Windows.Controls.TextBox logsText = LogsText;
			logsText.Text = logsText.Text + "\t| Finished!" + Environment.NewLine;
		});
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		DownloadList.Items.Add(LanguageList.SelectedItem);
	}

	private void Button_Click_1(object sender, RoutedEventArgs e)
	{
		DownloadList.Items.Remove(DownloadList.SelectedItem);
	}

	private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (System.Windows.Forms.MessageBox.Show("This button will add ALL languages to the download list", "ALERT", MessageBoxButtons.OKCancel) != DialogResult.OK)
		{
			return;
		}
		foreach (string item in (IEnumerable)LanguageList.Items)
		{
			DownloadList.Items.Add(item);
		}
	}

	private void Button_MouseRightButtonDown_1(object sender, MouseButtonEventArgs e)
	{
		if (System.Windows.Forms.MessageBox.Show("This button will remove ALL languages to the download list", "ALERT", MessageBoxButtons.OKCancel) == DialogResult.OK)
		{
			DownloadList.Items.Clear();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/SilverBullet;component/views/main/tools/tessdatadownloads.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((TessDataDownloads)target).Loaded += Page_Loaded;
			break;
		case 2:
			topMenu = (StackPanel)target;
			break;
		case 3:
			LanguageList = (System.Windows.Controls.ListBox)target;
			break;
		case 4:
			((System.Windows.Controls.Button)target).Click += Button_Click;
			((System.Windows.Controls.Button)target).MouseRightButtonDown += Button_MouseRightButtonDown;
			break;
		case 5:
			((System.Windows.Controls.Button)target).Click += Button_Click_1;
			((System.Windows.Controls.Button)target).MouseRightButtonDown += Button_MouseRightButtonDown_1;
			break;
		case 6:
			DownloadList = (System.Windows.Controls.ListBox)target;
			break;
		case 7:
			DownloadBtn = (System.Windows.Controls.Button)target;
			DownloadBtn.Click += DownloadBtn_Click;
			break;
		case 8:
			LogsText = (System.Windows.Controls.TextBox)target;
			break;
		case 9:
			progressBar = (System.Windows.Controls.ProgressBar)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
