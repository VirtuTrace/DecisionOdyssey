namespace Client.Models.DecisionElements.DecisionMatrix;

[Flags]
public enum MatrixDataType
{
    None = 0,
    Image = 1 << 0,
    Audio = 1 << 1,
    Video = 1 << 2,
    Text = 1 << 3
}