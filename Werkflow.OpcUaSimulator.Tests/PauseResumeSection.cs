using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PauseResumeSection
{
	public DateTime PausedAtUtc { get; set; }

	public DateTime ResumedAtUtc { get; set; }

	public string? LastNotificationBeforePause { get; set; }

	public string? FirstNotificationAfterResume { get; set; }

	public int NotificationsDuringPause { get; set; }

	public int PublishersBeforePause { get; set; }

	public int PublishersAfterResume { get; set; }

	public string Result { get; set; } = "Failed";
}
