namespace Client.Stats.DataStructures.TitledStats;

public class TitledNumericArrayStat : TitledStat
{
    public decimal[] Values { get; }
    
    public TitledNumericArrayStat(string title, decimal[] values) : base(title)
    {
        Values = values;
    }
    
    public TitledNumericArrayStat(string title, int[] values) : base(title)
    {
        Values = values.Select(x => (decimal)x).ToArray();
    }
}