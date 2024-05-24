using System.Text.Json.Serialization;

namespace Common.DataStructures;

public class DecisionMatrixStatsData : DecisionElementStatsData
{
    public DecisionMatrixStatsCellData[][] Stats { get; set; } = null!;
    public int[]? RowRatings { get; set; }
    public int Decision { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }

    [JsonIgnore]
    public DecisionMatrixStatsCellData this[int row, int col] => Stats[row][col];
    
    [JsonIgnore]
    public DecisionMatrixStatsCellData[] this[int row] => Stats[row];
}