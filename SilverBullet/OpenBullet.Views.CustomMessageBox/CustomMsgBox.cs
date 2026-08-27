using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace OpenBullet.Views.CustomMessageBox;

public sealed class CustomMsgBox
{
	private const string MessageBoxTitle = "Message";

	public static void Show(string message, bool isRTL = false)
	{
		using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
		customMsgBoxWindow.Title = "Message";
		customMsgBoxWindow.TitleLabel.Content = "Message";
		customMsgBoxWindow.Message.Text = message;
		customMsgBoxWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(3, 169, 244));
		customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
		if (isRTL)
		{
			customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
		}
		customMsgBoxWindow.BtnOk.Focus();
		customMsgBoxWindow.ShowDialog();
	}

	public static void Show(string message, string title, bool isRTL = false)
	{
		using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
		customMsgBoxWindow.Title = title;
		customMsgBoxWindow.TitleLabel.Content = title;
		customMsgBoxWindow.Message.Text = message;
		customMsgBoxWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(3, 169, 244));
		customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
		customMsgBoxWindow.MsgIcon.Kind = (PackIconMaterialKind)3508;
		((Control)(object)customMsgBoxWindow.MsgIcon).Foreground = Brushes.LightSteelBlue;
		if (isRTL)
		{
			customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
		}
		customMsgBoxWindow.BtnOk.Focus();
		customMsgBoxWindow.ShowDialog();
	}

	public static void ShowError(string errorMessage, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = "Error";
			customMsgBoxWindow.TitleLabel.Content = "Error";
			customMsgBoxWindow.Message.Text = errorMessage;
			customMsgBoxWindow.BorderBrush = Brushes.Red;
			customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
			customMsgBoxWindow.MsgIcon.Kind = (PackIconMaterialKind)1705;
			((Control)(object)customMsgBoxWindow.MsgIcon).Foreground = Brushes.Orange;
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
		}
		catch (Exception)
		{
			MessageBox.Show(errorMessage);
		}
	}

	public static void ShowError(string errorMessage, string errorTitle, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = errorTitle;
			customMsgBoxWindow.TitleLabel.Content = errorTitle;
			customMsgBoxWindow.Message.Text = errorMessage;
			customMsgBoxWindow.BorderBrush = Brushes.Red;
			customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
			customMsgBoxWindow.MsgIcon.Kind = (PackIconMaterialKind)1705;
			((Control)(object)customMsgBoxWindow.MsgIcon).Foreground = Brushes.Red;
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
		}
		catch (Exception)
		{
			MessageBox.Show(errorMessage);
		}
	}

	public static void ShowWarning(string warningMessage, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = "Warning";
			customMsgBoxWindow.TitleLabel.Content = "Warning";
			customMsgBoxWindow.Message.Text = warningMessage;
			customMsgBoxWindow.BorderBrush = Brushes.Orange;
			customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
			customMsgBoxWindow.MsgIcon.Kind = (PackIconMaterialKind)163;
			((Control)(object)customMsgBoxWindow.MsgIcon).Foreground = Brushes.Orange;
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
		}
		catch (Exception)
		{
			MessageBox.Show(warningMessage);
		}
	}

	public static void ShowWarning(string warningMessage, string warningTitle, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = warningTitle;
			customMsgBoxWindow.TitleLabel.Content = warningTitle;
			customMsgBoxWindow.Message.Text = warningMessage;
			customMsgBoxWindow.BorderBrush = Brushes.Orange;
			customMsgBoxWindow.BtnCancel.Visibility = Visibility.Collapsed;
			customMsgBoxWindow.MsgIcon.Kind = (PackIconMaterialKind)163;
			((Control)(object)customMsgBoxWindow.MsgIcon).Foreground = Brushes.Orange;
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
		}
		catch (Exception)
		{
			MessageBox.Show(warningMessage, warningTitle);
		}
	}

	public static MessageBoxResult ShowWithCancel(string message, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = "Message";
			customMsgBoxWindow.TitleLabel.Content = "Message";
			customMsgBoxWindow.Message.Text = message;
			customMsgBoxWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(3, 169, 244));
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
			return (customMsgBoxWindow.Result == MessageBoxResult.OK) ? MessageBoxResult.OK : MessageBoxResult.Cancel;
		}
		catch (Exception)
		{
			MessageBox.Show(message);
			return MessageBoxResult.Cancel;
		}
	}

	public static MessageBoxResult ShowWithCancel(string message, string title, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = title;
			customMsgBoxWindow.TitleLabel.Content = title;
			customMsgBoxWindow.Message.Text = message;
			customMsgBoxWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(3, 169, 244));
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
			return (customMsgBoxWindow.Result == MessageBoxResult.OK) ? MessageBoxResult.OK : MessageBoxResult.Cancel;
		}
		catch (Exception)
		{
			MessageBox.Show(message);
			return MessageBoxResult.Cancel;
		}
	}

	public static MessageBoxResult ShowWithCancel(string message, bool isError, bool isRTL = false)
	{
		try
		{
			using CustomMsgBoxWindow customMsgBoxWindow = new CustomMsgBoxWindow();
			customMsgBoxWindow.Title = "Message";
			customMsgBoxWindow.TitleLabel.Content = "Message";
			customMsgBoxWindow.Message.Text = message;
			if (isRTL)
			{
				customMsgBoxWindow.FlowDirection = FlowDirection.RightToLeft;
			}
			customMsgBoxWindow.BtnOk.Focus();
			customMsgBoxWindow.ShowDialog();
			return (customMsgBoxWindow.Result == MessageBoxResult.OK) ? MessageBoxResult.OK : MessageBoxResult.Cancel;
		}
		catch (Exception)
		{
			MessageBox.Show(message);
			return MessageBoxResult.Cancel;
		}
	}
}
