using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class ImageTracking(Stopwatch stopwatch) : MediaInteractionTracker(stopwatch)
{
    public override ImageTrackingData ExtractData()
    {
        return new ImageTrackingData
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}