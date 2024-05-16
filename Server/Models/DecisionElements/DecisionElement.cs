using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Models.DecisionElements;

public abstract class DecisionElement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }
    public Guid Guid { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime LastUpdated { get; set; }
    [StringLength(255)]
    public required string Name { get; set; }
    [StringLength(255)]
    public required string Filepath { get; set; }
    
    // Foreign key property
    public long UserId { get; set; }

    // Navigation property
    [JsonIgnore]
    [ForeignKey("UserId")]
    public User User { get; init; } = null!;
    
    public ICollection<User> Participants { get; set; } = null!; // Users who have access (to complete) to this DecisionElement
}