namespace Client.Stats.DataStructures.TitledStats;

public class TitledStringStat(string title, string value) : TitledStat(title)
{
    public string Value { get; } = value;
}