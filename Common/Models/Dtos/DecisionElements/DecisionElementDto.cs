namespace Common.Models.Dtos.DecisionElements;

public abstract class DecisionElementDto
{
    public required string Name { get; init; }
    public Guid Guid { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime LastUpdated { get; init; }
    public required string UserEmail { get; init; }
}