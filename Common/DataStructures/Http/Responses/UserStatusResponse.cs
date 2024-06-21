namespace Common.DataStructures.Http.Responses;

public class UserStatusResponse
{
    public string Role { get; set; } = string.Empty;
    public bool Locked { get; set; }
}