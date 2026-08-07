using System;
using System.Windows;
using System.Windows.Threading;

namespace Werkflow.OpcUaSimulator.App;

internal static class UiDispatcher
{
	public static void Run(Action action, DispatcherPriority priority = DispatcherPriority.Background)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val == null || val.CheckAccess())
		{
			action();
		}
		else
		{
			val.BeginInvoke((Delegate)action, priority, Array.Empty<object>());
		}
	}
}
