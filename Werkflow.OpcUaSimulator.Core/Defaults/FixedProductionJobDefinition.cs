namespace Werkflow.OpcUaSimulator.Core.Defaults;

public sealed class FixedProductionJobDefinition
{
	public required int CatalogIndex { get; init; }

	public required string JobName { get; init; }

	public required string PartName { get; init; }

	public required int TargetQuantity { get; init; }

	public required string MaterialName { get; init; }

	public required double MaterialThicknessMm { get; init; }

	public required string RecipeName { get; init; }

	public required string ProgramName { get; init; }

	public double ProcessLoadFactor => 0.72 + MaterialThicknessMm * 0.05;

	public double FeedRateFactor => 1.0 / (0.65 + MaterialThicknessMm * 0.06);
}
