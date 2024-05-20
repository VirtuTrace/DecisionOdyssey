namespace Common.DataStructures.Http.Responses;

public class StatusMessage
{
    public required string Message { get; init; }
    
    public static implicit operator StatusMessage(string message) => new() { Message = message };
    public static implicit operator string(StatusMessage statusMessage) => statusMessage.Message;
}