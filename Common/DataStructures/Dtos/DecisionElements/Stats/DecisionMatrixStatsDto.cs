namespace Common.DataStructures.Dtos.DecisionElements.Stats;

public class DecisionMatrixStatsDto
{
    public Guid Guid { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public string ParticipantEmail { get; init; } = null!;
    public DateTime StartTime { get; init; }
    public int Decision { get; set; } = -1;
    public long ElapsedMilliseconds { get; set; }
    public Guid MatrixGuid { get; set; }
}