namespace Client.Stats;

[Flags]
public enum StatsType
{
    None                = 0,
    Mean                = 1 << 0,
    Median              = 1 << 1,
    Mode                = 1 << 2,
    StandardDeviation   = 1 << 3,
    Variance            = 1 << 4,
    Range               = 1 << 5,
    Max                 = 1 << 6,
    Min                 = 1 << 7,
    All                 = Mean | Median | Mode | StandardDeviation | Variance | Range | Max | Min
}