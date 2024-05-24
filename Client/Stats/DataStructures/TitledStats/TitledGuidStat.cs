namespace Client.Stats.DataStructures.TitledStats;

public class TitledGuidStat(string title, Guid value) : TitledStat(title)
{
    public Guid Value { get; } = value;
}