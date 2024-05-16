namespace Server.Utility;

public static class ControllerConfig
{
    public const long MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10 GB
    public const string StatsDirectory = "./Stats";
}