namespace Client.Stats.DataStructures;

public struct BinInteraction
{
    internal long InteractionTime { get; }
    internal (int row, int col) Bin { get; }
    
    internal BinInteraction(long interactionTime, (int row, int col) bin)
    {
        InteractionTime = interactionTime;
        Bin = bin;
    }
}