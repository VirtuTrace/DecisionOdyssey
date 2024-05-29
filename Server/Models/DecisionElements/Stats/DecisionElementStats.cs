using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models.DecisionElements.Stats;

public abstract class DecisionElementStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }
    public Guid Guid { get; init; }
    [StringLength(255)] // Max length of a file path on Windows
    public string Filepath { get; set; } = null!;
    [StringLength(255)]
    public string ParticipantEmail { get; init; } = null!;
    public DateTime StartTime { get; init; }
    public long ElapsedMilliseconds { get; set; }
}