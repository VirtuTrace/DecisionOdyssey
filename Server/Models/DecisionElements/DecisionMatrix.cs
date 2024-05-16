using Common.Enums;
using Server.Models.DecisionElements.Stats;

namespace Server.Models.DecisionElements;

public class DecisionMatrix : DecisionElement
{
    public int NumRows { get; set; }
    public int NumColumns { get; set; }
    public MatrixFeatures Features { get; set; }
    
    public ICollection<DecisionMatrixStats> DecisionMatrixStats { get; set; } = null!;
}