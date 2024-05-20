using System.Diagnostics;

namespace Client.Models.DecisionElements.StatTracking;

public abstract class PlayableMediaTracking(Stopwatch stopwatch) : MediaInteractionTracker(stopwatch)
{
    private bool _recordedStartInteraction;

    public override void RecordStartInteraction()
    {
        base.RecordStartInteraction();
        _recordedStartInteraction = true;
    }
    
    public override void RecordEndInteraction()
    {
        if (!_recordedStartInteraction)
        {
            return;
        }
        
        base.RecordEndInteraction();
        _recordedStartInteraction = false;
    }
}