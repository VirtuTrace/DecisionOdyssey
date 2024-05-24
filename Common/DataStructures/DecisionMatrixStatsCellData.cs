using Common.DataStructures.Json.StatTracking;

namespace Common.DataStructures;

public class DecisionMatrixStatsCellData
{
    public VideoTrackingData? VideoTracking { get; set; }
    public ImageTrackingData? ImageTracking { get; set; }
    public AudioTrackingData? AudioTracking { get; set; }
    public TextTrackingData? TextTracking { get; set; }
    public List<long> Interactions { get; set; } = null!;
    public int Rating { get; set; }
}