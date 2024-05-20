using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class AudioTracking(Stopwatch stopwatch) : PlayableMediaTracking(stopwatch)
{
    public override AudioTrackingJson ToJson()
    {
        return new AudioTrackingJson
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}