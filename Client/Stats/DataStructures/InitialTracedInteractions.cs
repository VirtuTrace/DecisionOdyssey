namespace Client.Stats.DataStructures;

internal readonly struct InitialTracedInteractions
{
    internal Dictionary<(int, int), List<TracedInteraction>> InteractionMap { get; init; }
    internal List<BinInteraction> ChronologicalInteractions { get; init; }
    internal long TimeToInteraction { get; init; }
    internal decimal[] AttributeRanks { get; init; }
    internal int[] DimensionSelectionCounts { get; init; }
    internal int TotalDimensionSelectionCount { get; init; }
    internal int[] AlternativeSelectionCount { get; init; }
    internal int TotalAlternativeSelectionCount { get; init; }
    internal int SiDim { get; init; }
    internal int SiAlt { get; init; }
    internal int SiMix { get; init; }
}