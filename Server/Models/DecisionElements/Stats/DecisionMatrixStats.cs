using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Models.DecisionElements.Stats;

public class DecisionMatrixStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }
    public Guid Guid { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    [StringLength(255)] // Max length of a file path on Windows
    public string Filepath { get; init; } = null!;
    [StringLength(255)]
    public string ParticipantEmail { get; init; } = null!;
    public DateTime StartTime { get; init; }
    public int Decision { get; set; } = -1;
    public long ElapsedMilliseconds { get; set; }

    public long MatrixId { get; set; }
    
    [JsonIgnore]
    [ForeignKey("MatrixId")]
    public DecisionMatrix Matrix { get; set; } = null!;
    
    public long ParticipantId { get; set; }
    
    [JsonIgnore]
    [ForeignKey("ParticipantId")]
    public User Participant { get; set; } = null!;
}