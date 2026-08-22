namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class PressBrakeProgramCatalog
{
	private static readonly PressBrakeProgramDefinition[] Programs = BuildPrograms();

	public static IReadOnlyList<PressBrakeProgramDefinition> GetPrograms() => Programs;

	public static PressBrakeProgramDefinition GetProgram(int index) =>
		Programs[((index % Programs.Length) + Programs.Length) % Programs.Length];

	public static int ProgramCount => Programs.Length;

	private static PressBrakeProgramDefinition[] BuildPrograms()
	{
		return
		[
			new PressBrakeProgramDefinition
			{
				ProgramId = "PRG-7F2A",
				SetupDurationSeconds = 32.0,
				ToolChangeDurationSeconds = 48.0,
				ProgramTransitionSeconds = 22.0,
				Parts =
				[
					new PressBrakePartDefinition
					{
						PartId = "PRT-19C3",
						InterPartWaitSeconds = 5.2,
						OperatorWaitChance = 0.15,
						BendSteps =
						[
							Step(0, 92.0, 420.0, 245.0, 3.1, 1.9, 0.8, 2.4, 1.4),
							Step(1, 88.0, 310.0, 268.0, 2.6, 1.4, 0.6, 2.0, 0.9),
							Step(2, 45.0, 180.0, 198.0, 2.2, 1.1, 0.5, 1.8, 1.8)
						]
					},
					new PressBrakePartDefinition
					{
						PartId = "PRT-19C4",
						InterPartWaitSeconds = 3.8,
						BendSteps =
						[
							Step(0, 90.0, 395.0, 252.0),
							Step(1, 60.0, 260.0, 220.0, 2.4, 1.3, 0.7, 2.1, 1.2),
							Step(2, 30.0, 140.0, 185.0, 2.0, 1.0, 0.4, 1.7, 2.1),
							Step(3, 15.0, 95.0, 160.0, 1.8, 0.9, 0.3, 1.5, 0.7)
						]
					}
				]
			},
			new PressBrakeProgramDefinition
			{
				ProgramId = "PRG-4B91",
				SetupDurationSeconds = 24.0,
				ToolChangeDurationSeconds = 36.0,
				ProgramTransitionSeconds = 14.0,
				Parts =
				[
					new PressBrakePartDefinition
					{
						PartId = "PRT-82A1",
						InterPartWaitSeconds = 6.5,
						BendSteps =
						[
							Step(0, 110.0, 520.0, 310.0, 3.4, 2.2, 1.0, 2.6, 1.0),
							Step(1, 75.0, 340.0, 275.0, 2.8, 1.6, 0.8, 2.3, 1.6)
						]
					},
					new PressBrakePartDefinition
					{
						PartId = "PRT-82B7",
						InterPartWaitSeconds = 4.1,
						OperatorWaitChance = 0.2,
						BendSteps =
						[
							Step(0, 95.0, 460.0, 290.0),
							Step(1, 82.0, 380.0, 265.0, 2.5, 1.5, 0.6, 2.0, 0.8),
							Step(2, 55.0, 220.0, 230.0, 2.3, 1.2, 0.5, 1.9, 1.5),
							Step(3, 40.0, 150.0, 205.0, 2.1, 1.0, 0.4, 1.8, 1.1),
							Step(4, 25.0, 110.0, 175.0, 1.9, 0.9, 0.3, 1.6, 2.4)
						]
					}
				]
			},
			new PressBrakeProgramDefinition
			{
				ProgramId = "PRG-C3E8",
				SetupDurationSeconds = 38.0,
				ToolChangeDurationSeconds = 55.0,
				ProgramTransitionSeconds = 26.0,
				Parts =
				[
					new PressBrakePartDefinition
					{
						PartId = "PRT-5D02",
						InterPartWaitSeconds = 7.0,
						BendSteps =
						[
							Step(0, 88.0, 610.0, 285.0, 3.0, 1.8, 0.9, 2.5, 1.3),
							Step(1, 72.0, 480.0, 255.0, 2.7, 1.5, 0.7, 2.2, 1.0),
							Step(2, 58.0, 350.0, 240.0, 2.4, 1.3, 0.6, 2.0, 1.7),
							Step(3, 42.0, 240.0, 215.0, 2.2, 1.1, 0.5, 1.8, 0.9),
							Step(4, 28.0, 165.0, 190.0, 2.0, 1.0, 0.4, 1.7, 1.4),
							Step(5, 12.0, 90.0, 155.0, 1.7, 0.8, 0.3, 1.5, 2.0)
						]
					},
					new PressBrakePartDefinition
					{
						PartId = "PRT-5D11",
						InterPartWaitSeconds = 5.5,
						BendSteps =
						[
							Step(0, 100.0, 540.0, 300.0, 3.2, 2.0, 0.9, 2.4, 1.2),
							Step(1, 65.0, 290.0, 225.0, 2.5, 1.4, 0.6, 2.1, 1.8)
						]
					}
				]
			}
		];
	}

	private static PressBrakeBendStepDefinition Step(
		int index,
		double angle,
		double backgauge,
		double force,
		double approach = 2.8,
		double forming = 1.6,
		double hold = 0.7,
		double ret = 2.2,
		double interStep = 1.1) =>
		new()
		{
			StepIndex = index,
			TargetAngleDeg = angle,
			BackgaugePositionMm = backgauge,
			PeakForceKn = force,
			ApproachDurationSeconds = approach,
			FormingDurationSeconds = forming,
			HoldDurationSeconds = hold,
			ReturnDurationSeconds = ret,
			InterStepWaitSeconds = interStep
		};
}
