namespace Client.Stats.DataStructures;

public struct AggregateInteraction
{
    public decimal Mean { get; init; }
    public decimal Median { get; init; }
    public decimal Mode { get; init; }
    public decimal StandardDeviation { get; init; }
    public decimal Variance { get; init; }
    public decimal Range { get; init; }
    public decimal Max { get; init; }
    public decimal Min { get; init; }
}