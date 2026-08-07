using System;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalCorrelationEvaluator
{
	public const int MinimumSamples = 20;

	public static PhysicalCorrelationEvaluationResult Evaluate(PhysicalCorrelationEvaluationRequest request)
	{
		PhysicalCorrelationEvaluationResult physicalCorrelationEvaluationResult = new PhysicalCorrelationEvaluationResult
		{
			Pearson = request.Pearson,
			Spearman = request.Spearman,
			StrongestLag = request.StrongestLag,
			StrongestCrossCorrelation = request.StrongestCrossCorrelation,
			SampleCount = request.SampleCount,
			MinPearson = request.MinPearson,
			MaxPearson = request.MaxPearson,
			ExpectedDirection = request.ExpectedDirection,
			ExpectedLagSeconds = request.ExpectedLagSeconds
		};
		if (request.SampleCount < 20)
		{
			physicalCorrelationEvaluationResult.Result = "Failed";
			physicalCorrelationEvaluationResult.Reason = "Insufficient samples for correlation assessment.";
			return physicalCorrelationEvaluationResult;
		}
		double num = Math.Abs(request.Pearson);
		string expectedDirection = request.ExpectedDirection;
		if (1 == 0)
		{
		}
		bool flag = ((expectedDirection == "positive") ? (request.Pearson > 0.0) : ((!(expectedDirection == "negative")) ? (num > 0.05) : (request.Pearson < 0.0)));
		if (1 == 0)
		{
		}
		bool flag2 = flag;
		bool flag3 = num >= request.MinPearson;
		bool flag4 = num <= request.MaxPearson;
		int num2 = ((request.ExpectedLagSeconds == 0) ? 35 : Math.Max(12, request.ExpectedLagSeconds + 16));
		bool flag5 = Math.Abs(request.StrongestLag - request.ExpectedLagSeconds) <= num2;
		physicalCorrelationEvaluationResult.DirectionCorrect = flag2;
		physicalCorrelationEvaluationResult.MinStrengthMet = flag3;
		physicalCorrelationEvaluationResult.MaxStrengthMet = flag4;
		physicalCorrelationEvaluationResult.LagPlausible = flag5;
		if (!flag2)
		{
			physicalCorrelationEvaluationResult.Result = "Failed";
			physicalCorrelationEvaluationResult.Reason = "Correlation direction does not match expected relationship.";
			return physicalCorrelationEvaluationResult;
		}
		if (!flag3)
		{
			physicalCorrelationEvaluationResult.Result = "Failed";
			physicalCorrelationEvaluationResult.Reason = $"Pearson |{request.Pearson:F3}| below minimum {request.MinPearson:F2}.";
			return physicalCorrelationEvaluationResult;
		}
		if (!flag4)
		{
			physicalCorrelationEvaluationResult.Result = "Failed";
			physicalCorrelationEvaluationResult.Reason = $"Pearson |{request.Pearson:F3}| exceeds maximum {request.MaxPearson:F2}.";
			return physicalCorrelationEvaluationResult;
		}
		if (!flag5)
		{
			physicalCorrelationEvaluationResult.Result = "Failed";
			physicalCorrelationEvaluationResult.Reason = $"Lag {request.StrongestLag} outside plausible range for expected {request.ExpectedLagSeconds}s.";
			return physicalCorrelationEvaluationResult;
		}
		physicalCorrelationEvaluationResult.Result = "Passed";
		physicalCorrelationEvaluationResult.Reason = "Direction, strength and lag within expected bounds.";
		return physicalCorrelationEvaluationResult;
	}

	public static bool IsMandatoryPass(string result)
	{
		return result == "Passed";
	}
}
