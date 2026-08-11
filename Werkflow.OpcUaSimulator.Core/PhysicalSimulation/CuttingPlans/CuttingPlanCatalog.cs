using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;

public static class CuttingPlanCatalog
{
	public const int PlanCount = FixedSimulationCatalog.JobCount;

	private static readonly CuttingPlan[] Plans = BuildAll();

	public static CuttingPlan GetForCatalogIndex(int catalogIndex) => Plans[catalogIndex];

	public static CuttingPlan GetForJob(FixedProductionJobDefinition job) => Plans[job.CatalogIndex];

	public static IReadOnlyList<CuttingPlan> GetAll() => Plans;

	private static CuttingPlan[] BuildAll()
	{
		return new CuttingPlan[]
		{
			Build001(), Build002(), Build003(), Build004(), Build005(),
			Build006(), Build007(), Build008(), Build009(), Build010(),
			Build011(), Build012(), Build013(), Build014(), Build015(),
			Build016(), Build017(), Build018(), Build019(), Build020()
		};
	}

	private static CuttingPlan Wrap(int index, string jobId, List<CuttingPlanPart> parts) =>
		new()
		{
			PlanId = $"PLAN-{index + 1:D3}",
			JobCatalogIndex = index,
			JobId = jobId,
			Parts = parts
		};

	// PLAN-001: 12 small holders, outer + 2 inner holes
	private static CuttingPlan Build001()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 12; i++)
		{
			int col = i % 4;
			int row = i / 4;
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"H{i + 1}", 180 + col * 280, 120 + row * 220, 120, 80, innerHoles: 2));
		}
		return Wrap(0, "JOB-001", parts);
	}

	// PLAN-002: 6 large covers with inner cutout
	private static CuttingPlan Build002()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 6; i++)
		{
			int col = i % 3;
			int row = i / 3;
			double x = 200 + col * 520;
			double y = 180 + row * 380;
			var outer = CuttingPlanGeometry.RectangleContour(1, x, y, 420, 280, isInner: false);
			var inner = CuttingPlanGeometry.RectangleContour(0, x + 80, y + 50, 260, 180, isInner: true);
			parts.Add(new CuttingPlanPart
			{
				PartIndex = i,
				Label = $"C{i + 1}",
				Contours = [inner, outer]
			});
		}
		return Wrap(1, "JOB-002", parts);
	}

	// PLAN-003: 20 small flanges with center hole
	private static CuttingPlan Build003()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 20; i++)
		{
			int col = i % 5;
			int row = i / 5;
			double x = 150 + col * 220;
			double y = 100 + row * 160;
			var inner = CuttingPlanGeometry.RectangleContour(0, x + 28, y + 22, 44, 44, isInner: true);
			var outer = CuttingPlanGeometry.RectangleContour(1, x, y, 100, 70, isInner: false);
			parts.Add(new CuttingPlanPart { PartIndex = i, Label = $"F{i + 1}", Contours = [inner, outer] });
		}
		return Wrap(2, "JOB-003", parts);
	}

	// PLAN-004: 4 large housing plates, multiple inner contours
	private static CuttingPlan Build004()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 4; i++)
		{
			int col = i % 2;
			int row = i / 2;
			double x = 180 + col * 720;
			double y = 140 + row * 420;
			var contours = new List<CuttingPlanContour>
			{
				CuttingPlanGeometry.RectangleContour(0, x + 60, y + 50, 180, 120, isInner: true),
				CuttingPlanGeometry.RectangleContour(1, x + 320, y + 80, 140, 100, isInner: true),
				CuttingPlanGeometry.RectangleContour(2, x + 180, y + 220, 200, 80, isInner: true),
				CuttingPlanGeometry.RectangleContour(3, x, y, 620, 360, isInner: false)
			};
			parts.Add(new CuttingPlanPart { PartIndex = i, Label = $"G{i + 1}", Contours = contours });
		}
		return Wrap(3, "JOB-004", parts);
	}

	// PLAN-005: 10 L-shaped brackets
	private static CuttingPlan Build005()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 10; i++)
		{
			int col = i % 5;
			int row = i / 5;
			parts.Add(CuttingPlanGeometry.BuildLPart(i, $"L{i + 1}", 160 + col * 280, 120 + row * 320, 160, 140, 55));
		}
		return Wrap(4, "JOB-005", parts);
	}

	// PLAN-006: 8 frames
	private static CuttingPlan Build006()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 8; i++)
		{
			int col = i % 4;
			int row = i / 4;
			parts.Add(CuttingPlanGeometry.BuildFramePart(i, $"R{i + 1}", 200 + col * 340, 150 + row * 360, 260, 200, 28));
		}
		return Wrap(5, "JOB-006", parts);
	}

	// PLAN-007: 15 mixed rectangles varied sizes
	private static CuttingPlan Build007()
	{
		var parts = new List<CuttingPlanPart>();
		double[] widths = [90, 120, 150, 100, 130, 110, 140, 95, 125, 105, 135, 115, 145, 100, 120];
		double[] heights = [60, 80, 70, 90, 65, 75, 85, 70, 80, 60, 90, 75, 65, 85, 70];
		for (int i = 0; i < 15; i++)
		{
			int col = i % 5;
			int row = i / 5;
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"P{i + 1}", 140 + col * 220, 100 + row * 200, widths[i], heights[i], innerHoles: i % 3 == 0 ? 1 : 0));
		}
		return Wrap(6, "JOB-007", parts);
	}

	// PLAN-008: 3 very large panels
	private static CuttingPlan Build008()
	{
		var parts = new List<CuttingPlanPart>
		{
			CuttingPlanGeometry.BuildPart(0, "LP1", 120, 100, 900, 500, innerHoles: 4),
			CuttingPlanGeometry.BuildPart(1, "LP2", 1100, 120, 850, 480, innerHoles: 2),
			CuttingPlanGeometry.BuildPart(2, "LP3", 600, 700, 720, 420, innerHoles: 3)
		};
		return Wrap(7, "JOB-008", parts);
	}

	// PLAN-009: 16 tiny parts scattered
	private static CuttingPlan Build009()
	{
		var parts = new List<CuttingPlanPart>();
		double[] xs = [120, 380, 620, 900, 1180, 1500, 1780, 2100, 2400, 2600, 200, 550, 850, 1200, 1600, 2000];
		double[] ys = [100, 180, 120, 200, 140, 220, 160, 190, 130, 210, 450, 520, 480, 560, 500, 540];
		for (int i = 0; i < 16; i++)
		{
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"T{i + 1}", xs[i], ys[i], 70, 50, innerHoles: 0));
		}
		return Wrap(8, "JOB-009", parts);
	}

	// PLAN-010: 6 U-shaped channels (approximated)
	private static CuttingPlan Build010()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 6; i++)
		{
			int col = i % 3;
			int row = i / 3;
			double x = 220 + col * 480;
			double y = 160 + row * 400;
			double w = 320;
			double h = 200;
			double leg = 50;
			var vertices = new List<CuttingPlanPoint>
			{
				new() { X = x, Y = y },
				new() { X = x + w, Y = y },
				new() { X = x + w, Y = y + h },
				new() { X = x + w - leg, Y = y + h },
				new() { X = x + w - leg, Y = y + leg },
				new() { X = x + leg, Y = y + leg },
				new() { X = x + leg, Y = y + h },
				new() { X = x, Y = y + h }
			};
			parts.Add(new CuttingPlanPart
			{
				PartIndex = i,
				Label = $"U{i + 1}",
				Contours = [new CuttingPlanContour
				{
					ContourIndex = 0,
					IsInnerContour = false,
					Vertices = vertices,
					PiercePoint = new CuttingPlanPoint { X = x + 2, Y = y + 2 }
				}]
			});
		}
		return Wrap(9, "JOB-010", parts);
	}

	// PLAN-011: 9 medium plates with single hole
	private static CuttingPlan Build011()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 9; i++)
		{
			int col = i % 3;
			int row = i / 3;
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"M{i + 1}", 200 + col * 380, 140 + row * 280, 200, 140, innerHoles: 1));
		}
		return Wrap(10, "JOB-011", parts);
	}

	// PLAN-012: 5 large frames
	private static CuttingPlan Build012()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 5; i++)
		{
			double x = 150 + i * 520;
			parts.Add(CuttingPlanGeometry.BuildFramePart(i, $"FR{i + 1}", x, 200, 380, 280, 35));
		}
		return Wrap(11, "JOB-012", parts);
	}

	// PLAN-013: 14 narrow strips
	private static CuttingPlan Build013()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 14; i++)
		{
			int col = i % 7;
			int row = i / 7;
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"S{i + 1}", 120 + col * 200, 120 + row * 280, 160, 40, innerHoles: 0));
		}
		return Wrap(12, "JOB-013", parts);
	}

	// PLAN-014: 7 angled mounting brackets
	private static CuttingPlan Build014()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 7; i++)
		{
			double x = 180 + i * 360;
			parts.Add(CuttingPlanGeometry.BuildLPart(i, $"A{i + 1}", x, 200, 200, 160, 70));
		}
		return Wrap(13, "JOB-014", parts);
	}

	// PLAN-015: 11 mounting plates mixed
	private static CuttingPlan Build015()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 11; i++)
		{
			int col = i % 4;
			int row = i / 4;
			int holes = i % 4;
			parts.Add(CuttingPlanGeometry.BuildPart(i, $"MP{i + 1}", 160 + col * 300, 110 + row * 240, 140 + (i % 3) * 20, 100 + (i % 2) * 15, innerHoles: holes));
		}
		return Wrap(14, "JOB-015", parts);
	}

	// PLAN-016: 4 extra-large covers
	private static CuttingPlan Build016()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 4; i++)
		{
			int col = i % 2;
			int row = i / 2;
			double x = 140 + col * 760;
			double y = 120 + row * 380;
			var inner = CuttingPlanGeometry.RectangleContour(0, x + 100, y + 60, 320, 160, isInner: true);
			var outer = CuttingPlanGeometry.RectangleContour(1, x, y, 520, 280, isInner: false);
			parts.Add(new CuttingPlanPart { PartIndex = i, Label = $"XL{i + 1}", Contours = [inner, outer] });
		}
		return Wrap(15, "JOB-016", parts);
	}

	// PLAN-017: 18 hole-grid plates
	private static CuttingPlan Build017()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 18; i++)
		{
			int col = i % 6;
			int row = i / 6;
			double x = 130 + col * 210;
			double y = 100 + row * 200;
			var contours = new List<CuttingPlanContour>();
			for (int h = 0; h < 3; h++)
			{
				contours.Add(CuttingPlanGeometry.RectangleContour(h, x + 20 + h * 35, y + 25, 22, 22, isInner: true));
			}
			contours.Add(CuttingPlanGeometry.RectangleContour(3, x, y, 120, 90, isInner: false));
			parts.Add(new CuttingPlanPart { PartIndex = i, Label = $"HG{i + 1}", Contours = contours });
		}
		return Wrap(16, "JOB-017", parts);
	}

	// PLAN-018: 6 varied holders
	private static CuttingPlan Build018()
	{
		var parts = new List<CuttingPlanPart>
		{
			CuttingPlanGeometry.BuildPart(0, "V1", 200, 150, 280, 180, 2),
			CuttingPlanGeometry.BuildLPart(1, "V2", 600, 140, 200, 160, 60),
			CuttingPlanGeometry.BuildFramePart(2, "V3", 1000, 160, 300, 220, 25),
			CuttingPlanGeometry.BuildPart(3, "V4", 1400, 180, 240, 160, 1),
			CuttingPlanGeometry.BuildLPart(4, "V5", 1800, 150, 220, 200, 75),
			CuttingPlanGeometry.BuildPart(5, "V6", 2200, 200, 320, 200, 3)
		};
		return Wrap(17, "JOB-018", parts);
	}

	// PLAN-019: 13 small mixed layout
	private static CuttingPlan Build019()
	{
		var parts = new List<CuttingPlanPart>();
		for (int i = 0; i < 13; i++)
		{
			int col = i % 5;
			int row = i / 5;
			if (i % 5 == 2)
			{
				parts.Add(CuttingPlanGeometry.BuildLPart(i, $"X{i + 1}", 150 + col * 260, 110 + row * 220, 130, 110, 45));
			}
			else if (i % 5 == 4)
			{
				parts.Add(CuttingPlanGeometry.BuildFramePart(i, $"X{i + 1}", 150 + col * 260, 110 + row * 220, 150, 110, 18));
			}
			else
			{
				parts.Add(CuttingPlanGeometry.BuildPart(i, $"X{i + 1}", 150 + col * 260, 110 + row * 220, 110, 80, i % 2));
			}
		}
		return Wrap(18, "JOB-019", parts);
	}

	// PLAN-020: 2 very large console panels
	private static CuttingPlan Build020()
	{
		var parts = new List<CuttingPlanPart>
		{
			new CuttingPlanPart
			{
				PartIndex = 0,
				Label = "K1",
				Contours = [
					CuttingPlanGeometry.RectangleContour(0, 320, 180, 200, 140, isInner: true),
					CuttingPlanGeometry.RectangleContour(1, 680, 220, 160, 120, isInner: true),
					CuttingPlanGeometry.RectangleContour(2, 200, 520, 280, 100, isInner: true),
					CuttingPlanGeometry.RectangleContour(3, 100, 120, 1100, 680, isInner: false)
				]
			},
			new CuttingPlanPart
			{
				PartIndex = 1,
				Label = "K2",
				Contours = [
					CuttingPlanGeometry.RectangleContour(0, 1580, 200, 180, 180, isInner: true),
					CuttingPlanGeometry.RectangleContour(1, 1900, 350, 220, 160, isInner: true),
					CuttingPlanGeometry.RectangleContour(2, 1500, 120, 1000, 620, isInner: false)
				]
			}
		};
		return Wrap(19, "JOB-020", parts);
	}
}
