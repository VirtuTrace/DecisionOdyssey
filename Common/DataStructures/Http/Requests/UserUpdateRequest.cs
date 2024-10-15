using Common.DataStructures.Dtos;

namespace Common.DataStructures.Http.Requests;

public class UserUpdateRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? SecondaryEmail { get; set; }
    public string Password { get; set; } = "";

    public static UserUpdateRequest FromUserDto(UserDto user)
    {
        return new UserUpdateRequest
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            SecondaryEmail = user.SecondaryEmail
        };
    }
}