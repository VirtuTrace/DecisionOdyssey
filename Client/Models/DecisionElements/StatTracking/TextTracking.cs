using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class TextTracking(Stopwatch stopwatch) : MediaInteractionTracker(stopwatch)
{
    public override TextTrackingJson ToJson()
    {
        return new TextTrackingJson
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}