using System.Diagnostics;
using Client.Models.DecisionElements.StatTracking;

namespace Client.Models.DecisionElements.DecisionMatrix.Stats;

public class MatrixStatsCell
{
    public VideoTracking? VideoTracking { get; set; }
    public ImageTracking? ImageTracking { get; set; }
    public AudioTracking? AudioTracking { get; set; }
    public TextTracking? TextTracking { get; set; }
    public List<long> Interactions { get; set; } = [];
    public int Rating { get; set; }

    private readonly MatrixDataType _dataType;
    private readonly Stopwatch _stopwatch;
    
    public MatrixStatsCell(MatrixCell matrixCell, Stopwatch stopwatch)
    {
        _stopwatch = stopwatch;
        if (matrixCell.Contains(MatrixDataType.Video))
        {
            VideoTracking = new VideoTracking(stopwatch);
            _dataType |= MatrixDataType.Video;
        }
        else
        {
            if (matrixCell.Contains(MatrixDataType.Image))
            {
                ImageTracking = new ImageTracking(stopwatch);
                _dataType |= MatrixDataType.Image;
            }
            if (matrixCell.Contains(MatrixDataType.Audio))
            {
                AudioTracking = new AudioTracking(stopwatch);
                _dataType |= MatrixDataType.Audio;
            }
        }

        if (matrixCell.Contains(MatrixDataType.Text))
        {
            TextTracking = new TextTracking(stopwatch);
            _dataType |= MatrixDataType.Text;
        }
    }

    public void RecordStartInteraction()
    {
        ImageTracking?.RecordStartInteraction();
        TextTracking?.RecordStartInteraction();
        Interactions.Add(_stopwatch.ElapsedMilliseconds);
    }
    
    public void RecordEndInteraction()
    {
        VideoTracking?.RecordEndInteraction();
        ImageTracking?.RecordEndInteraction();
        AudioTracking?.RecordEndInteraction();
        TextTracking?.RecordEndInteraction();
    }
}