using System.Diagnostics;
using Common.DataStructures.Json.StatTracking;

namespace Client.Models.DecisionElements.StatTracking;

public class AudioTracking(Stopwatch stopwatch) : PlayableMediaTracking(stopwatch)
{
    public override AudioTrackingData ExtractData()
    {
        return new AudioTrackingData
        {
            StartTimes = StartTimes,
            EndTimes = EndTimes
        };
    }
}