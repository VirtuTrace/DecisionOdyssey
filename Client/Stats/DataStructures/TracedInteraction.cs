namespace Client.Stats.DataStructures;

public struct TracedInteraction
{
    internal long InteractionTime { get; }
    internal long InteractionIndex { get; }
    
    internal TracedInteraction(long interactionTime, long interactionIndex)
    {
        InteractionTime = interactionTime;
        InteractionIndex = interactionIndex;
    }
}