using System.Text.Json.Serialization;
using Common.Enums;

namespace Client.Models.DecisionElements.DecisionMatrix;

public class DecisionMatrixMetadata
{
    public required string Name { get; init; }
    public Guid Guid { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime LastUpdated { get; set; }
    public MatrixFeatures Features { get; init; }
    public required List<string> RowNames { get; init; }
    public required List<string> ColumnNames { get; init; }
    public int AllottedTime { get; init; }
    
    [JsonIgnore]
    public int RowCount => RowNames.Count;
    
    [JsonIgnore]
    public int ColumnCount => ColumnNames.Count;
}