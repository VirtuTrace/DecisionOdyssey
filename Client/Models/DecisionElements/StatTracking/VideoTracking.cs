using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class VideoTracking(Stopwatch stopwatch) : PlayableMediaTracking(stopwatch)
{
    public override VideoTrackingData ExtractData()
    {
        return new VideoTrackingData
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}