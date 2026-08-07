using System.Windows;
using Microsoft.Win32;
using Werkflow.OpcUaSimulator.Core.Interfaces;

namespace Werkflow.OpcUaSimulator.App.Services;

public sealed class DialogService : IDialogService
{
	public void ShowInfo(string title, string message)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	public void ShowWarning(string title, string message)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
	}

	public void ShowError(string title, string message)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	public bool ShowConfirmation(string title, string message)
	{
		return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
	}

	public string? ShowSaveFileDialog(string filter, string defaultFileName)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = filter,
			FileName = defaultFileName
		};
		return (saveFileDialog.ShowDialog() == true) ? saveFileDialog.FileName : null;
	}

	public string? ShowOpenFileDialog(string filter)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = filter
		};
		return (openFileDialog.ShowDialog() == true) ? openFileDialog.FileName : null;
	}
}
