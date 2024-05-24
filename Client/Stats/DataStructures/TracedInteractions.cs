namespace Client.Stats.DataStructures;

internal readonly struct TracedInteractions
{
    internal InitialTracedInteractions InitialTracedInteractions { get; init; }
    internal decimal[] AlternativesSelectionPercentage { get; init; }
    internal decimal[] DecisionFactorsSelectionPercentage { get; init; }
    internal int TotalBinsVisited { get; init; }
    internal decimal Si { get; init; }
    internal decimal[] DecisionFactorsSearchIndices { get; init; }
    internal decimal[] AlternativesSearchIndices { get; init; }
    internal decimal Coverage { get; init; }
    internal List<string> DecisionStrategies { get; init; }
    
    internal Dictionary<(int, int), List<TracedInteraction>> InteractionMap => InitialTracedInteractions.InteractionMap;
    internal List<BinInteraction> ChronologicalInteractions => InitialTracedInteractions.ChronologicalInteractions;
    internal long TimeToInteraction => InitialTracedInteractions.TimeToInteraction;
    internal decimal[] AttributeRanks => InitialTracedInteractions.AttributeRanks;
    internal int[] DecisionFactorSelectionCounts => InitialTracedInteractions.DimensionSelectionCounts;
    internal int TotalDecisionFactorSelectionCount => InitialTracedInteractions.TotalDimensionSelectionCount;
    internal int[] AlternativeSelectionCount => InitialTracedInteractions.AlternativeSelectionCount;
    internal int TotalAlternativeSelectionCount => InitialTracedInteractions.TotalAlternativeSelectionCount;
    internal int SiDim => InitialTracedInteractions.SiDim;
    internal int SiAlt => InitialTracedInteractions.SiAlt;
    internal int SiMix => InitialTracedInteractions.SiMix;
}