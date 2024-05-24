namespace Client.Stats.DataStructures.TitledStats;

public abstract class TitledStat(string title)
{
    public string Title { get; } = title;
}