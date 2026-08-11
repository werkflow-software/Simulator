using System;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class SimulationJob
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string PartName { get; set; } = string.Empty;

	public string JobName { get; set; } = string.Empty;

	public int TargetQuantity { get; set; }

	public int Priority { get; set; }

	public JobState Status { get; set; } = JobState.Pending;

	public Guid? AssignedMachineId { get; set; }

	public int ActualCounter { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? StartedAt { get; set; }

	public DateTime? CompletedAt { get; set; }

	public int CatalogIndex { get; set; }

	public string MaterialName { get; set; } = string.Empty;

	public double MaterialThicknessMm { get; set; }

	public string RecipeName { get; set; } = string.Empty;

	public string ProgramName { get; set; } = string.Empty;

	public SimulationJob Clone()
	{
		return new SimulationJob
		{
			Id = Guid.NewGuid(),
			PartName = PartName,
			JobName = JobName,
			TargetQuantity = TargetQuantity,
			Priority = Priority,
			Status = JobState.Pending,
			AssignedMachineId = null,
			ActualCounter = 0,
			CreatedAt = DateTime.UtcNow,
			StartedAt = null,
			CompletedAt = null,
			CatalogIndex = CatalogIndex,
			MaterialName = MaterialName,
			MaterialThicknessMm = MaterialThicknessMm,
			RecipeName = RecipeName,
			ProgramName = ProgramName
		};
	}
}
