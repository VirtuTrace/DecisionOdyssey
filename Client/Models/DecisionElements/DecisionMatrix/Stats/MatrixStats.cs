using System.Diagnostics;

namespace Client.Models.DecisionElements.DecisionMatrix.Stats;

public class MatrixStats
{
    public Guid Guid { get; } = Guid.NewGuid();
    public Guid MatrixGuid { get; set; }
    public string ParticipantEmail { get; set; } = null!;
    public MatrixStatsCell[][] Stats { get; }
    public int[]? RowRatings { get; set; }
    public DateTime StartTime { get; } = DateTime.Now;
    public int Decision { get; set; } = -1;
    
    private readonly Stopwatch _stopwatch = new();
    
    public int RowCount => Stats.Length;
    public int ColumnCount => Stats[0].Length;
    public long ElapsedMilliseconds
    {
        get
        {
            if(_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
            }
            return _stopwatch.ElapsedMilliseconds;
        }
    }

    public MatrixStatsCell this[int row, int col] => Stats[row][col];
    
    public MatrixStats(Matrix matrix)
    {
        MatrixGuid = matrix.Guid;
        Stats = new MatrixStatsCell[matrix.RowCount][];
        for (var row = 0; row < Stats.Length; row++)
        {
            Stats[row] = new MatrixStatsCell[matrix.ColumnCount];
            for(var column = 0; column < Stats[row].Length; column++)
            {
                Stats[row][column] = new MatrixStatsCell(matrix[row, column], _stopwatch);
            }
        }
        
        _stopwatch.Start();
    }
}