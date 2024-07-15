using System.Text.Json.Serialization;

namespace Common.DataStructures.Dtos;

public class UserDto
{
    public Guid Guid { get; set; }
    public required string Email { get; set; }
    public string? SecondaryEmail { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    
    [JsonIgnore]
    public bool IsLockedOut
    {
        get => LockoutEnd is not null && LockoutEnd > DateTimeOffset.Now;
        set => LockoutEnd = value ? DateTimeOffset.MaxValue : null;
    }
}