using System.Text.Json.Serialization;

namespace Common.DataStructures;

public class DecisionMatrixStatsData
{
    public Guid Guid { get; set;  }
    public Guid MatrixGuid { get; set; }
    public string ParticipantEmail { get; set; } = null!;
    public DecisionMatrixStatsCellData[][] Stats { get; set; } = null!;
    public int[]? RowRatings { get; set; }
    public DateTime StartTime { get; set; }
    public int Decision { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long ElapsedMilliseconds { get; set; }

    [JsonIgnore]
    public DecisionMatrixStatsCellData this[int row, int col] => Stats[row][col];
    
    [JsonIgnore]
    public DecisionMatrixStatsCellData[] this[int row] => Stats[row];
}