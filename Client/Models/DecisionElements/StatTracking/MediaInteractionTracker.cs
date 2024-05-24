using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public abstract class MediaInteractionTracker(Stopwatch stopwatch)
{
    public List<long> StartTimes { get; } = [];
    public List<long> EndTimes { get; } = [];

    public virtual void RecordStartInteraction()
    {
        StartTimes.Add(stopwatch.ElapsedMilliseconds);
    }
    
    public virtual void RecordEndInteraction()
    {
        EndTimes.Add(stopwatch.ElapsedMilliseconds);
    }

    public abstract MediaInteractionTrackerData ExtractData();
}