namespace Common.DataStructures.Dtos.DecisionElements.Stats;

public class DecisionMatrixStatsDto : DecisionElementStatsDto
{
    
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public int Decision { get; set; } = -1;
}