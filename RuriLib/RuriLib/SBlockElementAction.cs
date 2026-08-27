using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Media;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RuriLib.Functions.Files;
using RuriLib.LS;

namespace RuriLib;

public class SBlockElementAction : BlockBase
{
	private ElementLocator locator;

	private string elementString = "";

	private int elementIndex;

	private ElementAction action;

	private string input = "";

	private string outputVariable = "";

	private bool isCapture;

	private bool recursive;

	public ElementLocator Locator
	{
		get
		{
			return locator;
		}
		set
		{
			locator = value;
			OnPropertyChanged("Locator");
		}
	}

	public string ElementString
	{
		get
		{
			return elementString;
		}
		set
		{
			elementString = value;
			OnPropertyChanged("ElementString");
		}
	}

	public int ElementIndex
	{
		get
		{
			return elementIndex;
		}
		set
		{
			elementIndex = value;
			OnPropertyChanged("ElementIndex");
		}
	}

	public ElementAction Action
	{
		get
		{
			return action;
		}
		set
		{
			action = value;
			OnPropertyChanged("Action");
		}
	}

	public string Input
	{
		get
		{
			return input;
		}
		set
		{
			input = value;
			OnPropertyChanged("Input");
		}
	}

	public string OutputVariable
	{
		get
		{
			return outputVariable;
		}
		set
		{
			outputVariable = value;
			OnPropertyChanged("OutputVariable");
		}
	}

	public bool IsCapture
	{
		get
		{
			return isCapture;
		}
		set
		{
			isCapture = value;
			OnPropertyChanged("IsCapture");
		}
	}

	public bool Recursive
	{
		get
		{
			return recursive;
		}
		set
		{
			recursive = value;
			OnPropertyChanged("Recursive");
		}
	}

	public SBlockElementAction()
	{
		base.Label = "ELEMENT ACTION";
	}

	public override BlockBase FromLS(string line)
	{
		string text = line.Trim();
		if (text.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref text);
		}
		Locator = (ElementLocator)LineParser.ParseEnum(ref text, "LOCATOR", typeof(ElementLocator));
		ElementString = LineParser.ParseLiteral(ref text, "STRING");
		if (LineParser.Lookahead(ref text) == TokenType.Boolean)
		{
			LineParser.SetBool(ref text, this);
		}
		else if (LineParser.Lookahead(ref text) == TokenType.Integer)
		{
			ElementIndex = LineParser.ParseInt(ref text, "INDEX");
		}
		Action = (ElementAction)LineParser.ParseEnum(ref text, "ACTION", typeof(ElementAction));
		if (text == string.Empty)
		{
			return this;
		}
		if (LineParser.Lookahead(ref text) == TokenType.Literal)
		{
			Input = LineParser.ParseLiteral(ref text, "INPUT");
		}
		if (LineParser.ParseToken(ref text, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		try
		{
			string text2 = LineParser.ParseToken(ref text, TokenType.Parameter, essential: true);
			if (text2.ToUpper() == "VAR" || text2.ToUpper() == "CAP")
			{
				IsCapture = text2.ToUpper() == "CAP";
			}
		}
		catch
		{
			throw new ArgumentException("Invalid or missing variable type");
		}
		try
		{
			OutputVariable = LineParser.ParseToken(ref text, TokenType.Literal, essential: true);
			return this;
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("ELEMENTACTION").Token(Locator)
			.Literal(ElementString);
		if (Recursive)
		{
			blockWriter.Boolean(Recursive, "Recursive");
		}
		else if (ElementIndex != 0)
		{
			blockWriter.Integer(ElementIndex, "ElementIndex");
		}
		blockWriter.Indent().Token(Action).Literal(Input, "Input");
		if (!blockWriter.CheckDefault(OutputVariable, "OutputVariable"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(OutputVariable);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		if (data.Driver == null)
		{
			data.Log(new LogEntry("Open a browser first!", Colors.White));
			throw new Exception("Browser not open");
		}
		IWebElement webElement = null;
		ReadOnlyCollection<IWebElement> readOnlyCollection = null;
		try
		{
			if (action != ElementAction.WaitForElement)
			{
				readOnlyCollection = FindElements(data);
				webElement = readOnlyCollection[elementIndex];
			}
		}
		catch
		{
			data.Log(new LogEntry("Cannot find element on the page", Colors.White));
		}
		List<string> list = new List<string>();
		try
		{
			switch (action)
			{
			case ElementAction.Clear:
				webElement.Clear();
				break;
			case ElementAction.SendKeys:
				webElement.SendKeys(BlockBase.ReplaceValues(input, data));
				break;
			case ElementAction.Click:
				webElement.Click();
				BlockBase.UpdateSeleniumData(data);
				break;
			case ElementAction.Submit:
				webElement.Submit();
				BlockBase.UpdateSeleniumData(data);
				break;
			case ElementAction.SelectOptionByText:
				new SelectElement(webElement).SelectByText(BlockBase.ReplaceValues(input, data));
				break;
			case ElementAction.SelectOptionByIndex:
				new SelectElement(webElement).SelectByIndex(int.Parse(BlockBase.ReplaceValues(input, data)));
				break;
			case ElementAction.SelectOptionByValue:
				new SelectElement(webElement).SelectByValue(BlockBase.ReplaceValues(input, data));
				break;
			case ElementAction.GetText:
				if (recursive)
				{
					foreach (IWebElement item in readOnlyCollection)
					{
						list.Add(item.Text);
					}
				}
				else
				{
					list.Add(webElement.Text);
				}
				break;
			case ElementAction.GetAttribute:
				if (recursive)
				{
					foreach (IWebElement item2 in readOnlyCollection)
					{
						list.Add(item2.GetAttribute(BlockBase.ReplaceValues(input, data)));
					}
				}
				else
				{
					list.Add(webElement.GetAttribute(BlockBase.ReplaceValues(input, data)));
				}
				break;
			case ElementAction.IsDisplayed:
				list.Add(webElement.Displayed.ToString());
				break;
			case ElementAction.IsEnabled:
				list.Add(webElement.Enabled.ToString());
				break;
			case ElementAction.IsSelected:
				list.Add(webElement.Selected.ToString());
				break;
			case ElementAction.LocationX:
				list.Add(webElement.Location.X.ToString());
				break;
			case ElementAction.LocationY:
				list.Add(webElement.Location.Y.ToString());
				break;
			case ElementAction.SizeX:
				list.Add(webElement.Size.Width.ToString());
				break;
			case ElementAction.SizeY:
				list.Add(webElement.Size.Height.ToString());
				break;
			case ElementAction.Screenshot:
				Files.SaveScreenshot(GetElementScreenShot(data.Driver, webElement), data);
				break;
			case ElementAction.ScreenshotBase64:
			{
				Bitmap elementScreenShot = GetElementScreenShot(data.Driver, webElement);
				MemoryStream memoryStream = new MemoryStream();
				elementScreenShot.Save(memoryStream, ImageFormat.Jpeg);
				list.Add(Convert.ToBase64String(memoryStream.ToArray()));
				break;
			}
			case ElementAction.SwitchToFrame:
				data.Driver.SwitchTo().Frame(webElement);
				break;
			case ElementAction.WaitForElement:
			{
				int num3 = 0;
				int num4 = 10000;
				try
				{
					num4 = int.Parse(input) * 1000;
				}
				catch
				{
				}
				bool flag2 = false;
				while (num3 < num4)
				{
					try
					{
						readOnlyCollection = FindElements(data);
						webElement = readOnlyCollection[0];
						flag2 = true;
					}
					catch
					{
						num3 += 200;
						Thread.Sleep(200);
						continue;
					}
					break;
				}
				if (!flag2)
				{
					data.Log(new LogEntry("Timeout while waiting for element", Colors.White));
				}
				break;
			}
			case ElementAction.WaitForElementNotExists:
			{
				int num = 0;
				int num2 = 10000;
				try
				{
					num2 = int.Parse(input) * 1000;
				}
				catch
				{
				}
				bool flag = true;
				while (num < num2)
				{
					try
					{
						readOnlyCollection = FindElements(data);
						webElement = readOnlyCollection[0];
						flag = false;
						num += 200;
						Thread.Sleep(200);
					}
					catch
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					data.Log(new LogEntry("Timeout while waiting for element not exists", Colors.White));
				}
				break;
			}
			case ElementAction.SendKeysHuman:
			{
				string text = BlockBase.ReplaceValues(input, data);
				Random random = new Random();
				string text2 = text;
				for (int i = 0; i < text2.Length; i++)
				{
					webElement.SendKeys(text2[i].ToString());
					Thread.Sleep(random.Next(100, 300));
				}
				break;
			}
			}
		}
		catch
		{
			data.Log(new LogEntry("Cannot execute action on the element", Colors.White));
		}
		data.Log(new LogEntry($"Executed action {action} on the element with input {BlockBase.ReplaceValues(input, data)}", Colors.White));
		if (list.Count != 0)
		{
			BlockBase.InsertVariable(data, isCapture, recursive, list, outputVariable);
		}
	}

	public static Bitmap GetElementScreenShot(IWebDriver driver, IWebElement element)
	{
		Bitmap bitmap = Image.FromStream(new MemoryStream(((ITakesScreenshot)driver).GetScreenshot().AsByteArray)) as Bitmap;
		return bitmap.Clone(new Rectangle(element.Location, element.Size), bitmap.PixelFormat);
	}

	private ReadOnlyCollection<IWebElement> FindElements(BotData data)
	{
		return locator switch
		{
			ElementLocator.Id => data.Driver.FindElements(By.Id(BlockBase.ReplaceValues(elementString, data))), 
			ElementLocator.Class => data.Driver.FindElements(By.ClassName(BlockBase.ReplaceValues(elementString, data))), 
			ElementLocator.Name => data.Driver.FindElements(By.Name(BlockBase.ReplaceValues(elementString, data))), 
			ElementLocator.Tag => data.Driver.FindElements(By.TagName(BlockBase.ReplaceValues(elementString, data))), 
			ElementLocator.Selector => data.Driver.FindElements(By.CssSelector(BlockBase.ReplaceValues(elementString, data))), 
			ElementLocator.XPath => data.Driver.FindElements(By.XPath(BlockBase.ReplaceValues(elementString, data))), 
			_ => null, 
		};
	}
}
