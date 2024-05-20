namespace Common.DataStructures.Http.Responses;

public class ErrorMessage
{
    public required string Message { get; init; }
    
    public static implicit operator ErrorMessage(string message) => new() { Message = message };
    public static implicit operator string(ErrorMessage errorMessage) => errorMessage.Message;
}