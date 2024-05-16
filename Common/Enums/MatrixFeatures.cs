namespace Common.Enums;

[Flags]
public enum MatrixFeatures
{
    None                = 0,
    Timer               = 1 << 0,
    RowRating           = 1 << 1,
    ColumnRating        = 1 << 2,
    RowRandomization    = 1 << 3,
    ColumnRandomization = 1 << 4,
    Prompt              = 1 << 5
}