using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalPhaseSegmentRecorder
{
	private sealed class SegmentAccumulator
	{
		public Guid MachineId { get; init; }

		public string MachineName { get; init; } = string.Empty;

		public string ProfileId { get; init; } = string.Empty;

		public string Phase { get; init; } = string.Empty;

		public DateTimeOffset StartedAtUtc { get; init; }

		public PhysicalJobSegmentSnapshot JobSnapshot { get; init; } = new PhysicalJobSegmentSnapshot();

		public bool IsBendingProfile { get; init; }

		public int SampleCount { get; set; }

		public double LoadSum { get; set; }

		public double CurrentSum { get; set; }

		public double TempSum { get; set; }

		public double SpeedSum { get; set; }

		public double PressureSum { get; set; }

		public int PressureSamples { get; set; }

		public double ProcessPowerSum { get; set; }

		public int ProcessPowerSamples { get; set; }

		public double LoadMin { get; set; } = double.MaxValue;

		public double LoadMax { get; set; } = double.MinValue;

		public double TempMin { get; set; } = double.MaxValue;

		public double TempMax { get; set; } = double.MinValue;

		public PhysicalPhaseSegmentSnapshot ToSnapshot(DateTimeOffset endUtc)
		{
			bool flag = SampleCount > 0;
			return new PhysicalPhaseSegmentSnapshot
			{
				MachineId = MachineId,
				MachineName = MachineName,
				ProfileId = ProfileId,
				Phase = Phase,
				StartedAtUtc = StartedAtUtc,
				EndedAtUtc = endUtc,
				DurationSeconds = (endUtc - StartedAtUtc).TotalSeconds,
				JobId = JobSnapshot.JobId,
				JobName = JobSnapshot.JobName,
				PartName = JobSnapshot.PartName,
				TargetCounter = JobSnapshot.TargetCounter,
				ActualCounterAtStart = JobSnapshot.ActualCounterAtStart,
				SampleCount = SampleCount,
				AverageLoad = (flag ? new double?(LoadSum / (double)SampleCount) : ((double?)null)),
				AverageCurrent = (flag ? new double?(CurrentSum / (double)SampleCount) : ((double?)null)),
				AverageTemperature = (flag ? new double?(TempSum / (double)SampleCount) : ((double?)null)),
				AverageSpeed = (flag ? new double?(SpeedSum / (double)SampleCount) : ((double?)null)),
				AveragePressure = ((PressureSamples > 0) ? new double?(PressureSum / (double)PressureSamples) : ((double?)null)),
				AverageProcessPower = ((ProcessPowerSamples > 0) ? new double?(ProcessPowerSum / (double)ProcessPowerSamples) : ((double?)null)),
				MinimumLoad = ((SampleCount > 0 && LoadMin != double.MaxValue) ? new double?(LoadMin) : ((double?)null)),
				MaximumLoad = ((SampleCount > 0 && LoadMax != double.MinValue) ? new double?(LoadMax) : ((double?)null)),
				MinimumTemperature = ((SampleCount > 0 && TempMin != double.MaxValue) ? new double?(TempMin) : ((double?)null)),
				MaximumTemperature = ((SampleCount > 0 && TempMax != double.MaxValue) ? new double?(TempMax) : ((double?)null)),
				IsValid = flag
			};
		}
	}

	private readonly List<PhysicalPhaseSegmentSnapshot> _segments = new List<PhysicalPhaseSegmentSnapshot>();

	private SegmentAccumulator? _current;

	private ProcessPhase? _trackedPhase;

	private int _trackedJobIndex;

	public IReadOnlyList<PhysicalPhaseSegmentSnapshot> Segments => _segments;

	public bool HasInvalidSegments => _segments.Any((PhysicalPhaseSegmentSnapshot s) => !s.IsValid);

	public void Observe(PhysicalMachineSession session, DateTimeOffset timestamp)
	{
		ProcessPhase currentPhase = session.Simulation.CurrentPhase;
		int jobIndex = session.Simulation.Job.JobIndex;
		if (_current == null || currentPhase != _trackedPhase || jobIndex != _trackedJobIndex)
		{
			CloseCurrent(timestamp);
			StartSegment(session, currentPhase, timestamp);
			_trackedPhase = currentPhase;
			_trackedJobIndex = jobIndex;
		}
		AccumulateSample(session);
	}

	public void CloseCurrent(DateTimeOffset endUtc)
	{
		if (_current != null)
		{
			_segments.Add(_current.ToSnapshot(endUtc));
			_current = null;
		}
	}

	private void StartSegment(PhysicalMachineSession session, ProcessPhase phase, DateTimeOffset startUtc)
	{
		PhysicalJobState job = session.Simulation.Job;
		_current = new SegmentAccumulator
		{
			MachineId = session.MachineId,
			MachineName = session.MachineName,
			ProfileId = session.Profile.ProfileId,
			Phase = phase.ToString(),
			StartedAtUtc = startUtc,
			JobSnapshot = new PhysicalJobSegmentSnapshot
			{
				JobId = job.JobIndex,
				JobName = job.JobName,
				PartName = job.PartName,
				TargetCounter = job.TargetQuantity,
				ActualCounterAtStart = job.ProducedQuantity
			},
			IsBendingProfile = (session.Profile.ProfileId == "bending-hydraulic-machine-300")
		};
	}

	private void AccumulateSample(PhysicalMachineSession session)
	{
		if (_current == null)
		{
			return;
		}
		double? signalValue = GetSignalValue(session, "Axis01.Load");
		double? signalValue2 = GetSignalValue(session, "Axis01.MotorCurrent");
		double? signalValue3 = GetSignalValue(session, "Axis01.MotorTemperature");
		double? signalValue4 = GetSignalValue(session, "Axis01.Speed");
		double? num = (_current.IsBendingProfile ? GetSignalValue(session, "Hydraulic.SupplyPressure") : new double?(0.0));
		double? signalValue5 = GetSignalValue(session, "Process.PowerDemand");
		if (signalValue.HasValue || signalValue2.HasValue || signalValue3.HasValue || signalValue4.HasValue)
		{
			_current.SampleCount++;
			if (signalValue.HasValue)
			{
				_current.LoadSum += signalValue.Value;
				_current.LoadMin = Math.Min(_current.LoadMin, signalValue.Value);
				_current.LoadMax = Math.Max(_current.LoadMax, signalValue.Value);
			}
			if (signalValue2.HasValue)
			{
				_current.CurrentSum += signalValue2.Value;
			}
			if (signalValue3.HasValue)
			{
				_current.TempSum += signalValue3.Value;
				_current.TempMin = Math.Min(_current.TempMin, signalValue3.Value);
				_current.TempMax = Math.Max(_current.TempMax, signalValue3.Value);
			}
			if (signalValue4.HasValue)
			{
				_current.SpeedSum += signalValue4.Value;
			}
			if (num.HasValue)
			{
				_current.PressureSum += num.Value;
				_current.PressureSamples++;
			}
			if (signalValue5.HasValue)
			{
				_current.ProcessPowerSum += signalValue5.Value;
				_current.ProcessPowerSamples++;
			}
		}
	}

	private static double? GetSignalValue(PhysicalMachineSession session, string signalId)
	{
		return session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == signalId)?.CurrentValue;
	}
}
