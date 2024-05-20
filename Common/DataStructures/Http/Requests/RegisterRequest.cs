namespace Common.DataStructures.Http.Requests;

public class RegisterRequest : LoginRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}