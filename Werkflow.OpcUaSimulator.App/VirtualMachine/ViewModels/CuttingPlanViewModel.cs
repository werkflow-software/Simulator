using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

public sealed class CuttingPlanContourViewModel
{
	public required int ContourIndex { get; init; }

	public bool IsInnerContour { get; init; }

	public required IList<(double X, double Y)> Points { get; init; }

	public CuttingContourState State { get; set; } = CuttingContourState.Unprocessed;
}

public sealed class CuttingPlanPartViewModel
{
	public required int PartIndex { get; init; }

	public required string Label { get; init; }

	public CuttingPartState State { get; set; } = CuttingPartState.NotStarted;

	public IList<CuttingPlanContourViewModel> Contours { get; init; } = [];
}

public sealed class CuttingPlanViewModel : ObservableObject
{
	public string PlanId { get; set; } = "—";

	public string JobId { get; set; } = "—";

	public string PartName { get; set; } = "—";

	public string MaterialText { get; set; } = "—";

	public string ThicknessText { get; set; } = "—";

	public int PartsOnSheet { get; set; }

	public int PartsProcessedOnSheet { get; set; }

	public int CurrentSheetPartIndex { get; set; }

	public int CurrentContourIndex { get; set; }

	public int ContourCountOnPart { get; set; }

	public string CurrentPhaseText { get; set; } = "—";

	public double HeadX { get; set; }

	public double HeadY { get; set; }

	public double SegmentStartX { get; set; }

	public double SegmentStartY { get; set; }

	public bool ShowRapidLine { get; set; }

	public bool IsPiercing { get; set; }

	public double SheetWidth { get; set; } = 3000;

	public double SheetHeight { get; set; } = 1500;

	public double Zoom { get; set; } = 1.0;

	public string NextJobPreview { get; set; } = "—";

	public ObservableCollection<CuttingPlanPartViewModel> Parts { get; } = [];

	public string ProcessedPartsText => $"Bearbeitet: {PartsProcessedOnSheet} / {PartsOnSheet}";

	public string ContourProgressText => $"Kontur: {CurrentContourIndex} / {ContourCountOnPart}";

	public void NotifyDisplayRefresh() => OnPropertyChanged(string.Empty);

	public void ZoomIn()
	{
		Zoom = Math.Min(3.0, Zoom * 1.2);
		OnPropertyChanged(nameof(Zoom));
	}

	public void ZoomOut()
	{
		Zoom = Math.Max(0.5, Zoom / 1.2);
		OnPropertyChanged(nameof(Zoom));
	}

	public void FitSheet()
	{
		Zoom = 1.0;
		OnPropertyChanged(nameof(Zoom));
	}
}
