using Client.Stats.DataStructures;
using Client.Utility;
using Common.DataStructures;
using Common.DataStructures.Dtos.DecisionElements.Stats;

namespace Client.Stats;

public static class StatsUtility
{
    public static decimal GetPearsonsCorrelation(decimal[] x, decimal[] y)
    {
        int shortestArrayLength;
        if (x.Length == y.Length)
        {
            shortestArrayLength = x.Length;
        }
        else if (x.Length > y.Length)
        {
            shortestArrayLength = y.Length;
            Console.Error.WriteLine($"x has more items in it, the last {x.Length - shortestArrayLength} item(s) will be ignored");
        }
        else
        {
            shortestArrayLength = x.Length;
            Console.Error.WriteLine($"y has more items in it, the last {y.Length - shortestArrayLength} item(s) will be ignored");
        }

        var xy = new List<decimal>();
        var x2 = new List<decimal>();
        var y2 = new List<decimal>();
        
        for(var i = 0; i < shortestArrayLength; i++)
        {
            xy.Add(x[i] * y[i]);
            x2.Add(x[i] * x[i]);
            y2.Add(y[i] * y[i]);
        }

        var sumX = 0m;
        var sumY = 0m;
        var sumXY = 0m;
        var sumX2 = 0m;
        var sumY2 = 0m;
        
        for(var i = 0; i < shortestArrayLength; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += xy[i];
            sumX2 += x2[i];
            sumY2 += y2[i];
        }
        
        var step1 = (shortestArrayLength * sumXY) - (sumX * sumY);
        var step2 = (shortestArrayLength * sumX2) - (sumX * sumX);
        var step3 = (shortestArrayLength * sumY2) - (sumY * sumY);
        var step4 = Sqrt(step2 * step3);
        var answer = step1 / step4;
        
        return answer;
    }

    public static decimal Sqrt(decimal x, decimal? guess = null)
    {
        if (x == 0)
        {
            return 0;
        }
        
        while (true)
        {
            var ourGuess = guess.GetValueOrDefault(x / 2m);
            var result = x / ourGuess;
            var average = (ourGuess + result) / 2m;

            // This checks for the maximum precision possible with a decimal.
            if (average == ourGuess)
            {
                return average;
            }

            guess = average;
        }
    }
    
    internal static List<BinInteraction> SortInteractions(DecisionMatrixStatsData stats)
    {
        var bins = new List<BinInteraction>();
        for (var row = 0; row < stats.RowCount; row++)
        {
            for (var col = 0; col < stats.ColumnCount; col++)
            {
                var cell = stats[row, col];
                bins.AddRange(cell.Interactions.Select(interaction => new BinInteraction(interaction, (row, col))));
            }
        }
        
        bins.Sort((a, b) => a.InteractionTime.CompareTo(b.InteractionTime));
        return bins;
    }
    
    internal static TracedInteractions TraceInteractions(DecisionMatrixStatsData stats)
    {
        var interactions = SortInteractions(stats);
        var interactionMap = new Dictionary<(int, int), List<TracedInteraction>>();
        
        //var dimensionNames = stats.Ro
        var numDimensions = stats.RowCount;
        var numAlternatives = stats.ColumnCount;
        
        var initialTracedInteractions = InitialDecisionParsing(interactions, numDimensions, numAlternatives);
        var attributeRanks = initialTracedInteractions.AttributeRanks;
        var dimensionSelectionCounts = initialTracedInteractions.DimensionSelectionCounts;
        var totalDimensionSelectionCount = initialTracedInteractions.TotalDimensionSelectionCount;
        var alternativeSelectionCount = initialTracedInteractions.AlternativeSelectionCount;
        var totalAlternativeSelectionCount = initialTracedInteractions.TotalAlternativeSelectionCount;
        var siDim = initialTracedInteractions.SiDim;
        var siAlt = initialTracedInteractions.SiAlt;
        var siMix = initialTracedInteractions.SiMix;
        
        CalculateAverageAttributeRank(attributeRanks, numDimensions);

        var alternativesSelectionPercentage = CalculateSelectionPercentage(alternativeSelectionCount,
            totalAlternativeSelectionCount, numAlternatives);

        var dimensionsSelectionPercentage =
            CalculateSelectionPercentage(dimensionSelectionCounts, totalDimensionSelectionCount, numDimensions);
        
        var totalBinsVisited = CalculateTotalBinsVisited(stats);

        var si = CalculateSi(siAlt, siDim);
        Console.WriteLine($"siAlt: = {siAlt}");
        Console.WriteLine($"siDim: = {siDim}");
        
        var dimensionsSearchIndex = CalculateDimensionalSearchIndex(dimensionSelectionCounts,
            numDimensions, numAlternatives);
        var alternativesSearchIndex = CalculateDimensionalSearchIndex(alternativeSelectionCount,
            numAlternatives, numDimensions);

        var decisionStrategies = DetermineDecisionStrategies(attributeRanks, dimensionSelectionCounts, numDimensions,
            numAlternatives, siDim, siAlt, siMix, si);
        
        var coverage = Percentage((decimal)totalBinsVisited / (numDimensions * numAlternatives));
        
        
        return new TracedInteractions
        {
            InitialTracedInteractions = initialTracedInteractions,
            AlternativesSelectionPercentage = alternativesSelectionPercentage,
            DecisionFactorsSelectionPercentage = dimensionsSelectionPercentage,
            TotalBinsVisited = totalBinsVisited,
            Si = si,
            DecisionFactorsSearchIndices = dimensionsSearchIndex,
            AlternativesSearchIndices = alternativesSearchIndex,
            DecisionStrategies = decisionStrategies,
            Coverage = coverage
        };
    }
    
    private static InitialTracedInteractions InitialDecisionParsing(List<BinInteraction> interactions, int numDimensions, int numAlternatives)
    {
        var interactionMap = new Dictionary<(int, int), List<TracedInteraction>>();
        var attributeRanks = new decimal[numDimensions];
        var dimensionSelectionCounts = new int[numDimensions];
        var totalDimensionSelectionCount = 0;
        var alternativeSelectionCount = new int[numAlternatives];
        var totalAlternativeSelectionCount = 0;
        var siDim = 0;
        var siAlt = 0;
        var siMix = 0;
        
        var index = 0;
        var prevRow = -1;
        var prevCol = -1;
        foreach (var (i, interaction) in interactions.Enumerate())
        {
            if (!interactionMap.TryGetValue(interaction.Bin, out var value))
            {
                value = [];
                interactionMap[interaction.Bin] = value;
            }

            value.Add(new TracedInteraction(interaction.InteractionTime, i));
         
            index++;
            var (currRow, currCol) = interaction.Bin;
            attributeRanks[currRow] += index;
            // Need matrix data to get dimension names
            //Console.WriteLine($"Selected dimension [{}]"); //"Selected dimension [" + dimensions[dim_index] + "]."
            dimensionSelectionCounts[currRow]++;
            totalDimensionSelectionCount++;
            // Need matrix data to get alternative names
            //Console.WriteLine("Selected alternative [" + alternatives[alt_index] + "].");
            alternativeSelectionCount[currCol]++;
            totalAlternativeSelectionCount++;
            if (currRow == prevRow && currCol != prevCol)
            {
                siDim++;
                siMix = 0;
            }
            else if (currRow != prevRow && currCol == prevCol)
            {
                siAlt++;
                siMix = 0;
            }
            else if (currRow != prevRow && currCol != prevCol)
            {
                siMix++;
            }
            
            prevRow = currRow;
            prevCol = currCol;
        }

        return new InitialTracedInteractions
        {
            InteractionMap = interactionMap,
            ChronologicalInteractions = interactions,
            TimeToInteraction = interactions.Count > 0 
                ? interactions[^1].InteractionTime - interactions[0].InteractionTime 
                : -1,
            AttributeRanks = attributeRanks,
            DimensionSelectionCounts = dimensionSelectionCounts,
            TotalDimensionSelectionCount = totalDimensionSelectionCount,
            AlternativeSelectionCount = alternativeSelectionCount,
            TotalAlternativeSelectionCount = totalAlternativeSelectionCount,
            SiDim = siDim,
            SiAlt = siAlt,
            SiMix = siMix
        };
    }
    
    private static void CalculateAverageAttributeRank(decimal[] attributeRanks, int numDimensions)
    {
        for(var i = 0; i < numDimensions; i++)
        {
            attributeRanks[i] /= numDimensions;
        }
    }
    
    private static decimal[] CalculateSelectionPercentage(int[] selectionCount, int totalSelectionCount, int numElements)
    {
        var alternativesSelectionPercentage = new decimal[numElements];
        for(var i = 0; i < numElements; i++)
        {
            alternativesSelectionPercentage[i] = (decimal)selectionCount[i] / totalSelectionCount;
        }

        return alternativesSelectionPercentage;
    }
    
    private static int CalculateTotalBinsVisited(DecisionMatrixStatsData stats)
    {
        var totalBinsVisited = 0;
        for(var row = 0; row < stats.RowCount; row++)
        {
            for(var col = 0; col < stats.ColumnCount; col++)
            {
                if(stats[row, col].Interactions.Count > 0)
                {
                    totalBinsVisited++;
                }
            }
        }

        return totalBinsVisited;
    }
    
    private static decimal CalculateSi(int siAlt, int siDim)
    {
        var si = 1.0m;
        var siDenominator = siAlt + siDim;
        if (siDenominator > 0)
        {
            si = Percentage(((decimal)siAlt - siDim) / siDenominator);
        }
        return si;
    }
    
    private static decimal[] CalculateDimensionalSearchIndex(int[] selectionCounts, int numMajorDimensionElements, int numMinorDimensionElements)
    {
        var searchIndices = new decimal[numMajorDimensionElements];
        for(var i = 0; i < numMajorDimensionElements; i++)
        {
            var totalOtherDimensions = 0.0m;
            for(var j = 0; j < numMajorDimensionElements; j++)
            {
                if (j == i)
                {
                    continue;
                }

                totalOtherDimensions += selectionCounts[j];
                Console.WriteLine($" -- {selectionCounts[j]}");
            }
            
            var denominator = totalOtherDimensions / (numMajorDimensionElements - 1);
            var searchIndex = (decimal)numMinorDimensionElements; // TODO: Rename (currently only difference is lack of 's' after 'dimension')
            if (denominator != 0)
            {
                searchIndex = selectionCounts[i] / denominator;
            }
            searchIndices[i] = Percentage(searchIndex);
        }

        return searchIndices;
    }

    private static List<string> DetermineDecisionStrategies(decimal[] attributeRanks, int[] dimensionSelectionCounts,
        int numDimensions, int numAlternatives, int siDim, int siAlt, int siMix, decimal si)
    {
        var decisionStrategies = DecisionStrategies.None;
        if (si > 0)
        {
            var siRatio = (decimal)siDim / (siAlt + siMix);
            var maxRatio = ((decimal)(numDimensions - 1) * numAlternatives) / (numAlternatives - 1);
            Console.WriteLine($"OT/(AT+MT) = {siDim} / ({siAlt} + {siMix}) = {siRatio}");
            Console.WriteLine($"((a - 1) * o) / (o - 1) = (({numDimensions} - 1) * {numAlternatives}) / ({numAlternatives} - 1) = {maxRatio}");
            if (siRatio == maxRatio)
            {
                decisionStrategies |= DecisionStrategies.EqualWeights;
                decisionStrategies |= DecisionStrategies.LeastImportantMinimumHeuristic;
                decisionStrategies |= DecisionStrategies.LeastVarianceHeuristic;
                decisionStrategies |= DecisionStrategies.MultiAttributeUtilityModel;
            }
            else
            {
                decisionStrategies |= DecisionStrategies.Disjunctive;
                decisionStrategies |= DecisionStrategies.SatisficingHeuristic;
            }
        }
        else
        {
            var correlation = StatsUtility.GetPearsonsCorrelation(attributeRanks,
                dimensionSelectionCounts.Select(x => (decimal)x).ToArray());
            switch (correlation)
            {
                case 0.0m:
                    decisionStrategies |= DecisionStrategies.AdditiveDifference;
                    decisionStrategies |= DecisionStrategies.Dominance;
                    decisionStrategies |= DecisionStrategies.Majority;
                    decisionStrategies |= DecisionStrategies.MajorityOfConfirmingDimensions;
                    break;
                case < 0.0m:
                    decisionStrategies |= DecisionStrategies.EliminationByAspects;
                    decisionStrategies |= DecisionStrategies.Lexicographic;
                    decisionStrategies |= DecisionStrategies.RecognitionHeuristic;
                    break;
                default:
                    decisionStrategies |= DecisionStrategies.Unknown;
                    decisionStrategies |= DecisionStrategies.AdditiveDifference;
                    decisionStrategies |= DecisionStrategies.Dominance;
                    decisionStrategies |= DecisionStrategies.Majority;
                    decisionStrategies |= DecisionStrategies.MajorityOfConfirmingDimensions;
                    decisionStrategies |= DecisionStrategies.EliminationByAspects;
                    decisionStrategies |= DecisionStrategies.Lexicographic;
                    decisionStrategies |= DecisionStrategies.RecognitionHeuristic;
                    Console.WriteLine($"[Warning] Correlation between attribute ranks and number of boxes is positive [{correlation}]. This shouldn't happen.");
                    break;
            }
        }
        
        return decisionStrategies.GetNames();
    }
    
    private static decimal Percentage(decimal value)
    {
        return Math.Floor(value * 100.0m) / 100.0m;
    }
}