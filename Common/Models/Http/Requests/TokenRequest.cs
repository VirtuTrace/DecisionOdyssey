namespace Common.Models.Http.Requests;

public class TokenRequest
{
    public required string Token { get; set; }
    
    public static implicit operator TokenRequest(string token)
    {
        return new TokenRequest
        {
            Token = token
        };
    }
}