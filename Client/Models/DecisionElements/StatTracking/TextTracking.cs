using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class TextTracking(Stopwatch stopwatch) : MediaInteractionTracker(stopwatch)
{
    public override TextTrackingData ExtractData()
    {
        return new TextTrackingData
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}