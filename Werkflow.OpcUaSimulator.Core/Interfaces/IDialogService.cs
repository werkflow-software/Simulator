namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IDialogService
{
	void ShowInfo(string title, string message);

	void ShowWarning(string title, string message);

	void ShowError(string title, string message);

	bool ShowConfirmation(string title, string message);

	string? ShowSaveFileDialog(string filter, string defaultFileName);

	string? ShowOpenFileDialog(string filter);
}
