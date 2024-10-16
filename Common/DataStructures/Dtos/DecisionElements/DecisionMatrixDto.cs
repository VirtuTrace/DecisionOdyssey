﻿using Common.Enums;

namespace Common.DataStructures.Dtos.DecisionElements;

public class DecisionMatrixDto : DecisionElementDto
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public MatrixFeatures Features { get; init; }
}