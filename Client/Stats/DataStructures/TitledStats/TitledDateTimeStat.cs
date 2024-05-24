namespace Client.Stats.DataStructures.TitledStats;

public class TitledDateTimeStat(string title, DateTime value) : TitledStat(title)
{
    public DateTime Value { get; } = value;
}