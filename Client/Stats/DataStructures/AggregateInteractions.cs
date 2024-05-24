namespace Client.Stats.DataStructures;

public struct AggregateInteractions
{
    public AggregateInteraction[] AlternativesSelectionPercentage { get; init; }
    public AggregateInteraction[] DecisionFactorsSelectionPercentage { get; init; }
    public AggregateInteraction TotalBinsVisited { get; init; }
    public AggregateInteraction Si { get; init; }
    public AggregateInteraction[] DecisionFactorSearchIndices { get; init; }
    public AggregateInteraction[] AlternativesSearchIndices { get; init; }
    public Dictionary<string, int> DecisionStrategies { get; init; }
    
    public AggregateInteraction[] AttributeRanks { get; init; }
    public AggregateInteraction[] DecisionFactorSelectionCounts { get; init; }
    public AggregateInteraction TotalDimensionSelectionCount { get; init; }
    public AggregateInteraction[] AlternativeSelectionCount { get; init; }
    public AggregateInteraction TotalAlternativeSelectionCount { get; init; }
    public AggregateInteraction Coverage { get; init; }
    public AggregateInteraction SiDF { get; init; }
    public AggregateInteraction SiAlt { get; init; }
    public AggregateInteraction SiMix { get; init; }
}