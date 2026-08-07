using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.Tests;

internal sealed class CorrelationSeries
{
	private readonly Queue<double> _x = new Queue<double>();

	private readonly Queue<double> _y = new Queue<double>();

	private readonly int _capacity;

	public string PairId { get; }

	public int Count => _x.Count;

	public CorrelationSeries(string pairId, int capacity)
	{
		PairId = pairId;
		_capacity = capacity;
	}

	public void Add(double x, double y)
	{
		_x.Enqueue(x);
		_y.Enqueue(y);
		while (_x.Count > _capacity)
		{
			_x.Dequeue();
			_y.Dequeue();
		}
	}

	public double Pearson()
	{
		double[] array = _x.ToArray();
		double[] array2 = _y.ToArray();
		if (array.Length < 3)
		{
			return 0.0;
		}
		double num = array.Average();
		double num2 = array2.Average();
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			double num6 = array[i] - num;
			double num7 = array2[i] - num2;
			num3 += num6 * num7;
			num4 += num6 * num6;
			num5 += num7 * num7;
		}
		double num8 = Math.Sqrt(num4 * num5);
		return (num8 < 1E-12) ? 0.0 : (num3 / num8);
	}

	public double PearsonFirstDifferences()
	{
		double[] array = _x.ToArray();
		double[] array2 = _y.ToArray();
		if (array.Length < 4)
		{
			return 0.0;
		}
		double[] array3 = new double[array.Length - 1];
		double[] array4 = new double[array2.Length - 1];
		for (int i = 1; i < array.Length; i++)
		{
			array3[i - 1] = array[i] - array[i - 1];
			array4[i - 1] = array2[i] - array2[i - 1];
		}
		double num = array3.Average();
		double num2 = array4.Average();
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		for (int j = 0; j < array3.Length; j++)
		{
			double num6 = array3[j] - num;
			double num7 = array4[j] - num2;
			num3 += num6 * num7;
			num4 += num6 * num6;
			num5 += num7 * num7;
		}
		double num8 = Math.Sqrt(num4 * num5);
		return (num8 < 1E-12) ? 0.0 : (num3 / num8);
	}

	public double Spearman()
	{
		double[] array = _x.ToArray();
		double[] ys = _y.ToArray();
		if (array.Length < 3)
		{
			return 0.0;
		}
		return PearsonRank(array, ys);
	}

	public (int Lag, double Value) StrongestCrossCorrelation(int maxLag)
	{
		double[] xs = _x.ToArray();
		double[] ys = _y.ToArray();
		int item = 0;
		double num = double.NegativeInfinity;
		for (int i = -maxLag; i <= maxLag; i++)
		{
			double num2 = CrossCorrelationAtLag(xs, ys, i);
			if (num2 > num)
			{
				num = num2;
				item = i;
			}
		}
		return (Lag: item, Value: num);
	}

	private static double PearsonRank(double[] xs, double[] ys)
	{
		double[] array = Rank(xs);
		double[] array2 = Rank(ys);
		double num = array.Average();
		double num2 = array2.Average();
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			double num6 = array[i] - num;
			double num7 = array2[i] - num2;
			num3 += num6 * num7;
			num4 += num6 * num6;
			num5 += num7 * num7;
		}
		double num8 = Math.Sqrt(num4 * num5);
		return (num8 < 1E-12) ? 0.0 : (num3 / num8);
	}

	private static double[] Rank(double[] values)
	{
		(double, int)[] array = (from t in values.Select((double v, int i) => (v: v, i: i))
			orderby t.v
			select t).ToArray();
		double[] array2 = new double[values.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array2[array[j].Item2] = j + 1;
		}
		return array2;
	}

	private static double CrossCorrelationAtLag(double[] xs, double[] ys, int lag)
	{
		if (lag >= 0)
		{
			int num = Math.Min(xs.Length, ys.Length) - lag;
			if (num < 3)
			{
				return 0.0;
			}
			return PearsonSlice(xs.AsSpan(0, num), ys.AsSpan(lag, num));
		}
		lag = -lag;
		int num2 = Math.Min(xs.Length, ys.Length) - lag;
		if (num2 < 3)
		{
			return 0.0;
		}
		return PearsonSlice(xs.AsSpan(lag, num2), ys.AsSpan(0, num2));
	}

	private static double PearsonSlice(ReadOnlySpan<double> xs, ReadOnlySpan<double> ys)
	{
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < xs.Length; i++)
		{
			num += xs[i];
			num2 += ys[i];
		}
		num /= (double)xs.Length;
		num2 /= (double)ys.Length;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		for (int j = 0; j < xs.Length; j++)
		{
			double num6 = xs[j] - num;
			double num7 = ys[j] - num2;
			num3 += num6 * num7;
			num4 += num6 * num6;
			num5 += num7 * num7;
		}
		double num8 = Math.Sqrt(num4 * num5);
		return (num8 < 1E-12) ? 0.0 : (num3 / num8);
	}
}
