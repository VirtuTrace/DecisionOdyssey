namespace Client.Models.DecisionElements;

public abstract class DecisionElement
{
    public string Name { get; set; } = "";
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreationTime { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public virtual void Reset()
    {
        Name = "";
        Guid = Guid.NewGuid();
        CreationTime = DateTime.Now;
        LastUpdated = DateTime.Now;
    }
}