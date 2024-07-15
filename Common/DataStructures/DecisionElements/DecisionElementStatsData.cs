namespace Common.DataStructures;

public abstract class DecisionElementStatsData
{
    public Guid Guid { get; set;  }
    public Guid ElementGuid { get; set; }
    public string ParticipantEmail { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public long ElapsedMilliseconds { get; set; }
}