using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Models.DecisionElements.Stats;

public class DecisionMatrixStats : DecisionElementStats
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public int Decision { get; set; } = -1;
    public long MatrixId { get; set; }
    
    [ForeignKey("MatrixId")]
    public DecisionMatrix Matrix { get; set; } = null!;
    
    public long ParticipantId { get; set; }
    
    [ForeignKey("ParticipantId")]
    public User Participant { get; set; } = null!;
}