namespace Common.DataStructures.Http.Responses;

public class GuestAuthResponse : AuthResponse
{
    public required string GuestId { get; set; }
}