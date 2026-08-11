using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

public sealed class CuttingPlanCanvasControl : UserControl
{
	private readonly Canvas _canvas = new();
	private readonly Viewbox _viewbox;
	private CuttingPlanViewModel? _planVm;

	public CuttingPlanCanvasControl()
	{
		_viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Child = _canvas
		};
		_canvas.Width = VirtualMachineKinematicsConfig.WorkingAreaXMax;
		_canvas.Height = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		_canvas.Background = HmiVisualTheme.PlanSheetBg;
		Content = _viewbox;
	}

	public void Bind(CuttingPlanViewModel planVm)
	{
		_planVm = planVm;
		RedrawStatic();
		RedrawDynamic();
	}

	public void RefreshGeometry()
	{
		RedrawStatic();
		RedrawDynamic();
	}

	private void RedrawStatic()
	{
		if (_planVm == null)
		{
			return;
		}

		_canvas.Children.Clear();
		DrawGrid();
		DrawSheetBorder();

		foreach (CuttingPlanPartViewModel part in _planVm.Parts)
		{
			foreach (CuttingPlanContourViewModel contour in part.Contours)
			{
				DrawContour(contour, part);
			}

			DrawPartLabel(part);
		}
	}

	private void RedrawDynamic()
	{
		if (_planVm == null)
		{
			return;
		}

		RemoveDynamicElements();
		if (_planVm.ShowRapidLine)
		{
			DrawRapidLine(_planVm.SegmentStartX, _planVm.SegmentStartY, _planVm.HeadX, _planVm.HeadY);
		}

		DrawHeadMarker(_planVm.HeadX, _planVm.HeadY, _planVm.IsPiercing);
	}

	private void RemoveDynamicElements()
	{
		for (int i = _canvas.Children.Count - 1; i >= 0; i--)
		{
			if (_canvas.Children[i] is FrameworkElement fe && fe.Tag as string == "dynamic")
			{
				_canvas.Children.RemoveAt(i);
			}
		}
	}

	private void DrawGrid()
	{
		double w = VirtualMachineKinematicsConfig.WorkingAreaXMax;
		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		for (double x = 0; x <= w; x += 100)
		{
			bool major = x % 500 == 0;
			_canvas.Children.Add(MakeLine(x, 0, x, h, major ? HmiVisualTheme.PlanGridMajor : HmiVisualTheme.PlanGridMinor, major ? 1.2 : 0.6));
		}
		for (double y = 0; y <= h; y += 100)
		{
			bool major = y % 500 == 0;
			_canvas.Children.Add(MakeLine(0, y, w, y, major ? HmiVisualTheme.PlanGridMajor : HmiVisualTheme.PlanGridMinor, major ? 1.2 : 0.6));
		}

		foreach (double x in new[] { 0, 500, 1000, 1500, 2000, 2500, 3000 })
		{
			var lbl = MakeLabel($"{x:0}", 10);
			Canvas.SetLeft(lbl, x + 4);
			Canvas.SetTop(lbl, h - 18);
			_canvas.Children.Add(lbl);
		}
		foreach (double y in new[] { 0, 500, 1000, 1500 })
		{
			var lbl = MakeLabel($"{y:0}", 10);
			Canvas.SetLeft(lbl, 4);
			Canvas.SetTop(lbl, h - y - 14);
			_canvas.Children.Add(lbl);
		}
	}

	private void DrawSheetBorder()
	{
		double w = VirtualMachineKinematicsConfig.WorkingAreaXMax;
		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		var rect = new Rectangle
		{
			Width = w,
			Height = h,
			Stroke = HmiVisualTheme.PlanBorder,
			StrokeThickness = 2,
			Fill = Brushes.Transparent
		};
		_canvas.Children.Add(rect);
	}

	private void DrawContour(CuttingPlanContourViewModel contour, CuttingPlanPartViewModel part)
	{
		if (contour.Points.Count < 2)
		{
			return;
		}

		var poly = new Polyline
		{
			Stroke = BrushForContour(contour.State),
			StrokeThickness = contour.State == CuttingContourState.Active ? 2.8 : 1.4,
			Fill = part.State == CuttingPartState.Completed ? HmiVisualTheme.PlanPartCompletedFill : Brushes.Transparent,
			StrokeDashArray = contour.IsInnerContour ? new DoubleCollection([4, 3]) : null
		};

		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		foreach ((double x, double y) in contour.Points)
		{
			poly.Points.Add(new Point(x, h - y));
		}
		if (contour.Points.Count > 0)
		{
			(double fx, double fy) = contour.Points[0];
			poly.Points.Add(new Point(fx, h - fy));
		}

		_canvas.Children.Add(poly);
	}

	private void DrawPartLabel(CuttingPlanPartViewModel part)
	{
		if (part.Contours.Count == 0 || part.Contours[0].Points.Count == 0)
		{
			return;
		}

		(double x, double y) = part.Contours[0].Points[0];
		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		var lbl = MakeLabelBold($"{part.PartIndex + 1}", 11);
		Canvas.SetLeft(lbl, x + 6);
		Canvas.SetTop(lbl, h - y - 16);
		_canvas.Children.Add(lbl);
	}

	private void DrawRapidLine(double x1, double y1, double x2, double y2)
	{
		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		var line = MakeLine(x1, h - y1, x2, h - y2, HmiVisualTheme.PlanRapidLine, 1.2);
		line.StrokeDashArray = new DoubleCollection([6, 4]);
		line.Tag = "dynamic";
		_canvas.Children.Add(line);
	}

	private void DrawHeadMarker(double x, double y, bool piercing)
	{
		double h = VirtualMachineKinematicsConfig.WorkingAreaYMax;
		double cy = h - y;
		const double size = 14;
		var crossH = MakeLine(x - size, cy, x + size, cy, HmiVisualTheme.PlanHeadMarker, 1.5);
		var crossV = MakeLine(x, cy - size, x, cy + size, HmiVisualTheme.PlanHeadMarker, 1.5);
		var circle = new Ellipse
		{
			Width = 10,
			Height = 10,
			Stroke = piercing ? HmiVisualTheme.PlanPierceMarker : HmiVisualTheme.PlanHeadMarker,
			StrokeThickness = 2,
			Fill = piercing ? HmiVisualTheme.PlanPierceFill : Brushes.Transparent
		};
		Canvas.SetLeft(circle, x - 5);
		Canvas.SetTop(circle, cy - 5);
		crossH.Tag = "dynamic";
		crossV.Tag = "dynamic";
		circle.Tag = "dynamic";
		_canvas.Children.Add(crossH);
		_canvas.Children.Add(crossV);
		_canvas.Children.Add(circle);
	}

	private static Brush BrushForContour(CuttingContourState state) =>
		state switch
		{
			CuttingContourState.Active => HmiVisualTheme.PlanContourActive,
			CuttingContourState.Completed => HmiVisualTheme.PlanContourDone,
			_ => HmiVisualTheme.PlanContourIdle
		};

	private static Line MakeLine(double x1, double y1, double x2, double y2, Brush brush, double thickness)
	{
		return new Line
		{
			X1 = x1,
			Y1 = y1,
			X2 = x2,
			Y2 = y2,
			Stroke = brush,
			StrokeThickness = thickness
		};
	}

	private static TextBlock MakeLabel(string text, double size)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = HmiVisualTheme.PlanGridLabel,
			FontSize = size,
			FontWeight = FontWeights.Normal
		};
	}

	private static TextBlock MakeLabelBold(string text, double size)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = HmiVisualTheme.PlanGridLabel,
			FontSize = size,
			FontWeight = FontWeights.Bold
		};
	}
}
