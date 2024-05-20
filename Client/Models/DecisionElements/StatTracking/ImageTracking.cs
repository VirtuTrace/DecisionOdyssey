using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class ImageTracking(Stopwatch stopwatch) : MediaInteractionTracker(stopwatch)
{
    public override ImageTrackingJson ToJson()
    {
        return new ImageTrackingJson
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}