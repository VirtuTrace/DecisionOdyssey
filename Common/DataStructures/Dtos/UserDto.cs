namespace Common.DataStructures.Dtos;

public class UserDto
{
    public Guid Guid { get; set; }
    public required string Email { get; set; }
    public string? SecondaryEmail { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}