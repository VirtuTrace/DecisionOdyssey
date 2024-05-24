namespace Client.Stats.DataStructures.TitledStats;

public class TitledNumericStat(string title, decimal value) : TitledStat(title)
{
    public decimal Value { get; } = value;
}