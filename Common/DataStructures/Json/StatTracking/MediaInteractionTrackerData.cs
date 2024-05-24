namespace Common.DataStructures.Json.StatTracking;

public abstract class MediaInteractionTrackerData
{
    public List<long> StartTimes { get; set; } = null!;
    public List<long> EndTimes { get; set; } = null!;
}