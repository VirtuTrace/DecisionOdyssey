namespace Client.Stats.DataStructures;

public class IntermediateAggregateInteraction(int numValues)
{
    public decimal[] Values { get; init; } = new decimal[numValues];

    public void AddValue(int index, decimal value)
    {
        Values[index] = value;
    }

    public AggregateInteraction Aggregate(bool population = false)
    {
        if (Values.Length == 1)
        {
            population = true;
        }
        var sortedValues = Values.OrderBy(v => v).ToArray();
        var min = sortedValues[0];
        var max = sortedValues[^1];
        var range = max - min;
        var median = sortedValues.Length % 2 == 0 ? (sortedValues[sortedValues.Length / 2] + sortedValues[sortedValues.Length / 2 - 1]) / 2 : sortedValues[sortedValues.Length / 2];
        
        var highestModeCount = 0;
        var currentModeCount = 0;
        var mode = sortedValues[0];
        var mean = 0m;
        foreach (var value in sortedValues)
        {
            mean += value;
            if(value == mode)
            {
                currentModeCount++;
            }
            else
            {
                if(currentModeCount > highestModeCount)
                {
                    highestModeCount = currentModeCount;
                    mode = value;
                }
                currentModeCount = 1;
            }
        }
        mean /= sortedValues.Length;
        
        var sumOfSquares = sortedValues
            .Select(value => value - mean)
            .Select(deviation => deviation * deviation)
            .Sum();
        var variance = population ? sumOfSquares / sortedValues.Length : sumOfSquares / (sortedValues.Length - 1);
        var standardDeviation = StatsUtility.Sqrt(variance);
        
        return new AggregateInteraction
        {
            Mean = mean,
            Median = median,
            Mode = mode,
            StandardDeviation = standardDeviation,
            Variance = variance,
            Range = range,
            Max = max,
            Min = min
        };
    }
}