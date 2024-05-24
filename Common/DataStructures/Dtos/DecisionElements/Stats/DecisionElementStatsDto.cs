namespace Common.DataStructures.Dtos.DecisionElements.Stats;

public abstract class DecisionElementStatsDto
{
    public Guid Guid { get; init; }
    public Guid ElementGuid { get; set; }
    public string ParticipantEmail { get; init; } = null!;
    public DateTime StartTime { get; init; }
    public long ElapsedMilliseconds { get; set; }
}