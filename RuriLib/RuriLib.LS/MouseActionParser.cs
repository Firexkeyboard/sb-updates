using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;

namespace RuriLib.LS;

internal class MouseActionParser
{
	public static Action Parse(string line, BotData data)
	{
		string input = line.Trim();
		Actions actions = null;
		try
		{
			actions = new Actions(data.Driver);
		}
		catch
		{
			throw new Exception("No Browser initialized!");
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		string text = "";
		int num7 = 1;
		int num8 = 1;
		int num9 = 0;
		IWebElement webElement = null;
		IWebElement webElement2 = null;
		Line line2 = null;
		while (input != string.Empty)
		{
			switch (LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper())
			{
			case "SPAWN":
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num3 = LineParser.ParseInt(ref input, "X");
					num4 = LineParser.ParseInt(ref input, "Y");
				}
				else if (LineParser.Lookahead(ref input) == TokenType.Literal)
				{
					num3 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "X"), data));
					num4 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "Y"), data));
				}
				SpawnDiv(data.Driver, num3, num4, LineParser.ParseLiteral(ref input, "ID"));
				break;
			case "CLICK":
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.Click();
				}
				else
				{
					actions.Click(ParseElement(ref input, data));
				}
				break;
			case "CLICKANDHOLD":
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.ClickAndHold();
				}
				else
				{
					actions.ClickAndHold(ParseElement(ref input, data));
				}
				break;
			case "RIGHTCLICK":
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.ContextClick();
				}
				else
				{
					actions.ContextClick(ParseElement(ref input, data));
				}
				break;
			case "DOUBLECLICK":
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.DoubleClick();
				}
				else
				{
					actions.DoubleClick(ParseElement(ref input, data));
				}
				break;
			case "DRAGANDDROP":
				webElement = ParseElement(ref input, data);
				LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
				webElement2 = ParseElement(ref input, data);
				actions.DragAndDrop(webElement, webElement2);
				break;
			case "DRAGANDDROPWITHOFFSET":
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num = LineParser.ParseInt(ref input, "OFFSET X");
					num2 = LineParser.ParseInt(ref input, "OFFSET Y");
				}
				else if (LineParser.Lookahead(ref input) == TokenType.Literal)
				{
					num = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "OFFSET X"), data));
					num2 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "OFFSET Y"), data));
				}
				actions.DragAndDropToOffset(ParseElement(ref input, data), num, num2);
				break;
			case "KEYDOWN":
				text = LineParser.ParseLiteral(ref input, "KEY", replace: true, data);
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.KeyDown(text);
				}
				else
				{
					actions.KeyDown(ParseElement(ref input, data), text);
				}
				break;
			case "KEYUP":
				text = LineParser.ParseLiteral(ref input, "KEY", replace: true, data);
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.KeyUp(text);
				}
				else
				{
					actions.KeyUp(ParseElement(ref input, data), text);
				}
				break;
			case "MOVEBY":
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num = LineParser.ParseInt(ref input, "OFFSET X");
					num2 = LineParser.ParseInt(ref input, "OFFSET Y");
				}
				else if (LineParser.Lookahead(ref input) == TokenType.Literal)
				{
					num = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "OFFSET X"), data));
					num2 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "OFFSET Y"), data));
				}
				actions.MoveByOffset(num, num2);
				break;
			case "MOVETO":
				actions.MoveToElement(ParseElement(ref input, data));
				break;
			case "RELEASE":
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.Release();
				}
				else
				{
					actions.Release(ParseElement(ref input, data));
				}
				break;
			case "SENDKEYS":
				text = LineParser.ParseLiteral(ref input, "KEY", replace: true, data);
				if (!LineParser.CheckIdentifier(ref input, "ELEMENT"))
				{
					actions.SendKeys(text);
				}
				else
				{
					actions.SendKeys(ParseElement(ref input, data), text);
				}
				break;
			case "DRAWPOINTS":
			{
				num = LineParser.ParseInt(ref input, "MAX WIDTH");
				num2 = LineParser.ParseInt(ref input, "MAX HEIGHT");
				int num10 = LineParser.ParseInt(ref input, "AMOUNT");
				Random random = new Random();
				int num11 = 0;
				int num12 = 0;
				actions.MoveToElement(data.Driver.FindElement(By.TagName("body")), num3, num4);
				List<Point> list = new List<Point>();
				for (int j = 0; j < num10; j++)
				{
					int num13 = random.Next(0, num);
					int num14 = random.Next(0, num2);
					actions.MoveByOffset(num13 - num11, num14 - num12);
					num11 = num13;
					num12 = num14;
					list.Add(new Point(num13, num14));
				}
				if (data.GlobalSettings.Selenium.DrawMouseMovement)
				{
					DrawRedDots(data.Driver, list.ToArray(), 5);
				}
				break;
			}
			case "DRAWLINE":
			{
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num3 = LineParser.ParseInt(ref input, "X1");
					num4 = LineParser.ParseInt(ref input, "Y1");
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					num5 = LineParser.ParseInt(ref input, "X2");
					num6 = LineParser.ParseInt(ref input, "Y2");
				}
				else if (LineParser.Lookahead(ref input) == TokenType.Literal)
				{
					num3 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "X1"), data));
					num4 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "Y1"), data));
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					num5 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "X2"), data));
					num6 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "Y2"), data));
				}
				else
				{
					webElement = ParseElement(ref input, data);
					num3 = webElement.Location.X;
					num4 = webElement.Location.Y;
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					webElement2 = ParseElement(ref input, data);
					num5 = webElement2.Location.X;
					num6 = webElement2.Location.Y;
				}
				LineParser.EnsureIdentifier(ref input, ":");
				num9 = LineParser.ParseInt(ref input, "QUANTITY");
				actions.MoveToElement(data.Driver.FindElement(By.TagName("body")), num3, num4);
				line2 = new Line(new Point(num3, num4), new Point(num5, num6));
				if (data.GlobalSettings.Selenium.DrawMouseMovement)
				{
					DrawRedDots(data.Driver, line2.getPoints(num9), 5);
				}
				Point[] offsets = line2.getOffsets(num9);
				for (int i = 0; i < offsets.Length; i++)
				{
					Point point2 = offsets[i];
					actions.MoveByOffset(point2.X, point2.Y);
				}
				break;
			}
			case "DRAWLINEHUMAN":
			{
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num3 = LineParser.ParseInt(ref input, "X1");
					num4 = LineParser.ParseInt(ref input, "Y1");
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					num5 = LineParser.ParseInt(ref input, "X2");
					num6 = LineParser.ParseInt(ref input, "Y2");
				}
				else if (LineParser.Lookahead(ref input) == TokenType.Literal)
				{
					num3 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "X1"), data));
					num4 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "Y1"), data));
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					num5 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "X2"), data));
					num6 = int.Parse(BlockBase.ReplaceValues(LineParser.ParseLiteral(ref input, "Y2"), data));
				}
				else
				{
					webElement = ParseElement(ref input, data);
					num3 = webElement.Location.X;
					num4 = webElement.Location.Y;
					LineParser.ParseToken(ref input, TokenType.Arrow, essential: true);
					webElement2 = ParseElement(ref input, data);
					num5 = webElement2.Location.X;
					num6 = webElement2.Location.Y;
				}
				LineParser.EnsureIdentifier(ref input, ":");
				num9 = LineParser.ParseInt(ref input, "QUANTITY");
				if (LineParser.Lookahead(ref input) == TokenType.Integer)
				{
					num7 = LineParser.ParseInt(ref input, "GRAVITY");
					num8 = LineParser.ParseInt(ref input, "WIND");
				}
				actions.MoveToElement(data.Driver.FindElement(By.TagName("body")), num3, num4);
				line2 = new Line(new Point(num3, num4), new Point(num5, num6));
				Point[] array = ShrinkArray(line2.HumanWindMouse(num3, num4, num5, num6, num7, num8, 1.0), num9);
				if (data.GlobalSettings.Selenium.DrawMouseMovement)
				{
					DrawRedDots(data.Driver, array, 5);
				}
				Point[] offsets = GetOffsets(array);
				for (int i = 0; i < offsets.Length; i++)
				{
					Point point = offsets[i];
					actions.MoveByOffset(point.X, point.Y);
				}
				break;
			}
			}
		}
		return delegate
		{
			actions.Build();
			actions.Perform();
			data.Log(new LogEntry("Executed Mouse Actions", Colors.White));
		};
	}

	private static void DrawRedDot(RemoteWebDriver driver, int x, int y, int thickness)
	{
		try
		{
			((IJavaScriptExecutor)driver).ExecuteScript(RedDotScript(x, y, thickness), Array.Empty<object>());
		}
		catch
		{
		}
	}

	public static Point[] ShrinkArray(Point[] originArray, int targetSize)
	{
		Random random = new Random();
		List<Point> list = originArray.ToList();
		while (list.Count > targetSize)
		{
			list.RemoveAt(random.Next(1, list.Count - 2));
		}
		return list.ToArray();
	}

	private static Point[] GetOffsets(Point[] originArray)
	{
		List<Point> list = new List<Point>();
		Point point = originArray[0];
		for (int i = 0; i < originArray.Length; i++)
		{
			Point point2 = originArray[i];
			list.Add(new Point(point2.X - point.X, point2.Y - point.Y));
			point = point2;
		}
		return list.ToArray();
	}

	private static string RedDotScript(int x, int y, int thickness, string id = "reddot")
	{
		return "\t\tvar div = document.createElement('div');\t\tdiv.style.backgroundColor = 'red';       div.id = '" + id + "';\t\tdiv.style.position = 'absolute';\t\tdiv.style.left = '" + x + "px';       div.style.top = '" + y + "px';\t    div.style.height = '" + thickness + "px';\t\tdiv.style.width = '" + thickness + "px';\t\tdocument.getElementsByTagName('body')[0].appendChild(div);";
	}

	private static void SpawnDiv(WebDriver driver, int x, int y, string id)
	{
		try
		{
			((IJavaScriptExecutor)driver).ExecuteScript(RedDotScript(x, y, 1, id), Array.Empty<object>());
		}
		catch
		{
		}
	}

	public static void DrawRedDots(WebDriver driver, Point[] points, int thickness)
	{
		string text = "";
		for (int i = 0; i < points.Count(); i++)
		{
			text += RedDotScript(points[i].X, points[i].Y, thickness);
		}
		try
		{
			((IJavaScriptExecutor)driver).ExecuteScript(text, Array.Empty<object>());
		}
		catch
		{
		}
	}

	public static IWebElement ParseElement(ref string input, BotData data)
	{
		LineParser.EnsureIdentifier(ref input, "ELEMENT");
		ElementLocator elementLocator = (ElementLocator)LineParser.ParseEnum(ref input, "Element Locator", typeof(ElementLocator));
		string text = LineParser.ParseLiteral(ref input, "Element Identifier");
		int index = 0;
		if (LineParser.Lookahead(ref input) == TokenType.Integer)
		{
			index = LineParser.ParseInt(ref input, "Element Index");
		}
		return elementLocator switch
		{
			ElementLocator.Id => data.Driver.FindElements(By.Id(text))[index], 
			ElementLocator.Class => data.Driver.FindElements(By.ClassName(text))[index], 
			ElementLocator.Name => data.Driver.FindElements(By.Name(text))[index], 
			ElementLocator.Selector => data.Driver.FindElements(By.CssSelector(text))[index], 
			ElementLocator.Tag => data.Driver.FindElements(By.TagName(text))[index], 
			ElementLocator.XPath => data.Driver.FindElements(By.XPath(text))[index], 
			_ => throw new Exception("Element not found on the page"), 
		};
	}
}
