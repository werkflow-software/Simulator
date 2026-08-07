using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;

public sealed class DelayRingBuffer
{
	private readonly double[] _samples;

	private readonly TimeSpan _sampleInterval;

	private int _head;

	private int _count;

	public DelayRingBuffer(TimeSpan maxDelay, TimeSpan sampleInterval)
	{
		_sampleInterval = ((sampleInterval <= TimeSpan.Zero) ? TimeSpan.FromMilliseconds(100.0) : sampleInterval);
		int num = Math.Max(2, (int)Math.Ceiling(maxDelay.TotalMilliseconds / _sampleInterval.TotalMilliseconds) + 1);
		_samples = new double[num];
	}

	public void Push(double value)
	{
		_samples[_head] = value;
		_head = (_head + 1) % _samples.Length;
		if (_count < _samples.Length)
		{
			_count++;
		}
	}

	public double GetDelayed(TimeSpan delay)
	{
		if (_count == 0)
		{
			return 0.0;
		}
		int value = (int)Math.Round(delay.TotalMilliseconds / _sampleInterval.TotalMilliseconds);
		value = Math.Clamp(value, 0, _count - 1);
		int num = (_head - 1 - value + _samples.Length) % _samples.Length;
		return _samples[num];
	}
}
