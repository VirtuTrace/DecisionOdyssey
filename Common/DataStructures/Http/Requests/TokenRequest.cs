namespace Common.DataStructures.Http.Requests;

public class TokenRequest
{
    public required string AccessToken { get; set; }
    
    public static implicit operator TokenRequest(string token)
    {
        return new TokenRequest
        {
            AccessToken = token
        };
    }
}