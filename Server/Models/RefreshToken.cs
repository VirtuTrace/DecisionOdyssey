using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [StringLength(255)]
    public required string Token { get; set; }
    public DateTimeOffset ExpiryTime { get; set; }
    public bool Valid { get; set; }
    
    public long UserId { get; set; }
    [ForeignKey("UserId")]
    public required User User { get; set; }
}