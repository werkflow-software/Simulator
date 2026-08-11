namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class LaserMotionPhaseLabels
{
	public static string ToGerman(LaserMotionPhase phase)
	{
		if (1 == 0)
		{
		}
		string result = phase switch
		{
			LaserMotionPhase.Idle => "LEERLAUF",
			LaserMotionPhase.Setup => "EINRICHTEN",
			LaserMotionPhase.JobChange => "JOBWECHSEL",
			LaserMotionPhase.NozzleChange => "DÜSENWECHSEL",
			LaserMotionPhase.RapidPositioning => "POSITIONIEREN",
			LaserMotionPhase.Repositioning => "POSITIONIEREN",
			LaserMotionPhase.Piercing => "EINSTECHEN",
			LaserMotionPhase.Cutting => "SCHNEIDEN",
			LaserMotionPhase.Recovery => "WIEDERHERSTELLUNG",
			_ => "—"
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string ToEnglish(LaserMotionPhase phase) =>
		phase switch
		{
			LaserMotionPhase.Idle => "IDLE",
			LaserMotionPhase.Setup => "SETUP",
			LaserMotionPhase.JobChange => "JOB CHANGE",
			LaserMotionPhase.NozzleChange => "NOZZLE CHANGE",
			LaserMotionPhase.RapidPositioning => "RAPID",
			LaserMotionPhase.Repositioning => "RAPID",
			LaserMotionPhase.Piercing => "PIERCING",
			LaserMotionPhase.Cutting => "CUTTING",
			LaserMotionPhase.Recovery => "RECOVERY",
			_ => "—"
		};
}
