using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class VideoTracking(Stopwatch stopwatch) : PlayableMediaTracking(stopwatch)
{
    public override VideoTrackingJson ToJson()
    {
        return new VideoTrackingJson
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}