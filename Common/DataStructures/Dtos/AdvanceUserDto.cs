using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Common.DataStructures.Dtos;

public class AdvanceUserDto : UserDto
{
    public required string Role { get; set; }

    [JsonConstructor]
    public AdvanceUserDto()
    {
        
    }
    
    [SetsRequiredMembers]
    public AdvanceUserDto(UserDto userDto, string role)
    {
        Guid = userDto.Guid;
        Email = userDto.Email;
        SecondaryEmail = userDto.SecondaryEmail;
        FirstName = userDto.FirstName;
        LastName = userDto.LastName;
        LockoutEnd = userDto.LockoutEnd;
        Role = role;
    }
}