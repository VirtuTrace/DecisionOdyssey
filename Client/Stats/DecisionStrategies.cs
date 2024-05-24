namespace Client.Stats;

[Flags]
internal enum DecisionStrategies
{
    None                            = 0,
    Unknown                         = 1 << 0,
    EqualWeights                    = 1 << 1,
    LeastImportantMinimumHeuristic  = 1 << 2,
    LeastVarianceHeuristic          = 1 << 3,
    MultiAttributeUtilityModel      = 1 << 4,
    Disjunctive                     = 1 << 5,
    SatisficingHeuristic            = 1 << 6,
    AdditiveDifference              = 1 << 7,
    Dominance                       = 1 << 8,
    Majority                        = 1 << 9,
    MajorityOfConfirmingDimensions  = 1 << 10,
    EliminationByAspects            = 1 << 11,
    Lexicographic                   = 1 << 12,
    RecognitionHeuristic            = 1 << 13
}

internal static class DecisionStrategiesExtensionMethods
{
    internal static List<string> GetNames(this DecisionStrategies strategies)
    {
        return (from strategy in (DecisionStrategies[])Enum.GetValues(typeof(DecisionStrategies))
            where strategy != DecisionStrategies.None
            where strategies.HasFlag(strategy)
            select strategy.GetName()).ToList();
    }
    
    private static string GetName(this DecisionStrategies strategy)
    {
        return strategy switch
        {
            DecisionStrategies.Unknown =>
                "Unknown (because the correlation between attribute ranks and the number of cells opened was positive)",
            DecisionStrategies.EqualWeights => "Equal Weights Strategy (EQW)",
            DecisionStrategies.LeastImportantMinimumHeuristic => "Least-Important Minimum Heuristic (LIM)",
            DecisionStrategies.LeastVarianceHeuristic => "Least-Variance Heuristic (LVA)",
            DecisionStrategies.MultiAttributeUtilityModel => "Multiattribute Utility Model (MAU)",
            DecisionStrategies.Disjunctive => "Disjunctive Strategy (DIS)",
            DecisionStrategies.SatisficingHeuristic => "Satisficing Heuristic (SAT)",
            DecisionStrategies.AdditiveDifference => "Additive Difference Strategy (ADD)",
            DecisionStrategies.Dominance => "Dominance Strategy (DOM)",
            DecisionStrategies.Majority => "Majority Strategy (MAJ)",
            DecisionStrategies.MajorityOfConfirmingDimensions => "Majority of Confirming Dimensions Strategy (MCD)",
            DecisionStrategies.EliminationByAspects => "Elimination-By-Aspects Strategy (EBA)",
            DecisionStrategies.Lexicographic => "Lexicographic Strategy (LEX)",
            DecisionStrategies.RecognitionHeuristic => "Recognition Heuristic (REC)",
            _ => "Tracing Error"
        };
    }
}