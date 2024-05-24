using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Client.Excel;
using Client.Models.DecisionElements.DecisionMatrix;
using Client.Stats.DataStructures;
using Client.Stats.DataStructures.TitledStats;
using Client.Utility;
using Common.DataStructures;

namespace Client.Stats;

public class StatsCreator : IDisposable
{
    private readonly ExcelCreator _excelCreator = new("Overview");
    private readonly List<(string, XDocument)> _matrixTraces = [];

    /// <summary>
    ///     Save the Excel file to a MemoryStream and return it. The document writer is disposed after this method is called.
    /// </summary>
    /// <returns>The MemoryStream containing the Excel file</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MemoryStream Save()
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var xlsxStream = _excelCreator.Save();
            foreach (var (name, matrixTrace) in _matrixTraces)
            {
                var xmlEntry = archive.CreateEntry(name);
                using var entryStream = xmlEntry.Open();
                matrixTrace.Save(entryStream);
            }

            var xlsxEntry = archive.CreateEntry("stats.xlsx");
            using (var entryStream = xlsxEntry.Open())
            {
                xlsxStream.CopyTo(entryStream);
            }
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public void AddStats(DecisionMatrix matrix, List<DecisionMatrixStatsData> stats)
    {
        var interactions = new List<TracedInteractions>();
        foreach (var statsJson in stats)
        {
            var sheetIndex = _excelCreator.AddSheet(statsJson.Guid.ToString());
            var interaction = AddUserStats(statsJson, sheetIndex);
            interactions.Add(interaction);
            _excelCreator.AutoSizeColumn(sheetIndex, 1);
            var matrixTrace = ExportMatrixTrace(matrix, interaction, statsJson);
            var matrixTraceName = $"{statsJson.StartTime:yy-MM-dd}_{statsJson.Guid}.xml";
            _matrixTraces.Add((matrixTraceName, matrixTrace));
        }
        AddAggregateStats(matrix.Name, interactions, matrix);
    }

    private static XDocument ExportMatrixTrace(DecisionMatrix matrix, TracedInteractions tracedInteractions, DecisionMatrixStatsData stats)
    {
        var root = new XElement("decision_matrix", new XAttribute("name", matrix.Name));
        var labels = new XElement("labels");
        root.Add(labels);
        var alternatives = new XElement("alternatives");
        labels.Add(alternatives);
        foreach (var alternative in matrix.ColumnNames)
        {
            alternatives.Add(new XElement("alternative", alternative));
        }
        var dimensions = new XElement("dimensions");
        labels.Add(dimensions);
        foreach (var dimension in matrix.RowNames)
        {
            dimensions.Add(new XElement("dimension", dimension));
        }
        var interactions = new XElement("interactions");
        root.Add(interactions);
        foreach (var interaction in tracedInteractions.ChronologicalInteractions)
        {
            var (row, col) = interaction.Bin;
            var alternative = matrix.ColumnNames[col];
            var dimension = matrix.RowNames[row];
            var interactionElement = new XElement("info",
                new XAttribute("alternative", alternative),
                new XAttribute("dimension", dimension),
                new XAttribute("timestamp", interaction.InteractionTime));
            interactions.Add(interactionElement);
        }
        var decision = new XElement("choice", 
            new XAttribute("alternative", matrix.ColumnNames[stats.Decision]),
            new XAttribute("timestamp", stats.ElapsedMilliseconds));
        interactions.Add(decision);
        var matrixTrace = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        return matrixTrace;
    }
    
    private void AddAggregateStats(string matrixName, List<TracedInteractions> stats, DecisionMatrix matrix)
    {
        const int sheetIndex = 0; // Overview sheet contains the aggregate stats
        var aggregateStats = AggregateStats(stats);
        var nextRow = 1;
        _excelCreator.InsertHeader(sheetIndex, "=== Metadata ===", nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Matrix Name", nextRow, 1);
        _excelCreator.InsertString(sheetIndex, matrixName, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Number of Participants", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, stats.Count, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Number of Alternatives", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, stats[0].AlternativesSelectionPercentage.Length, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Number of Dimension Factors", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, stats[0].DecisionFactorsSelectionPercentage.Length, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertHeader(sheetIndex, "=== Aggregate Stats ===", nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Alternatives Selection [%]", nextRow, 1);
        nextRow++;
        
        var alternatives = matrix.ColumnNames;
        foreach (var (i, interaction) in aggregateStats.AlternativesSelectionPercentage.Enumerate())
        {
            _excelCreator.InsertString(sheetIndex, $"Alternative {i+1}: {alternatives[i]}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.Mean, nextRow, 2);
            nextRow++;
        }
        _excelCreator.InsertString(sheetIndex, "Decision Factor Selection [%]", nextRow, 1);
        nextRow++;
        
        var decisionFactors = matrix.RowNames;
        foreach (var (i, interaction) in aggregateStats.DecisionFactorsSelectionPercentage.Enumerate())
        {
            _excelCreator.InsertString(sheetIndex, $"Decision Factor {i+1}: {decisionFactors[i]}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.Mean, nextRow, 2);
            nextRow++;
        }
        
        _excelCreator.InsertString(sheetIndex, "Mean # of Total Unique Bins Visited", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, aggregateStats.TotalBinsVisited.Mean, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Standard Deviation of Total Unique Bins Visited", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, aggregateStats.TotalBinsVisited.StandardDeviation, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Max Value for Total Unique Bins Visited", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, aggregateStats.TotalBinsVisited.Max, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, "Min Value for Total Unique Bins Visited", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, aggregateStats.TotalBinsVisited.Min, nextRow, 2);
        nextRow++;
        
        nextRow = AddAggregateInteraction(sheetIndex, nextRow, aggregateStats.Si, "SI");
        nextRow = AddAggregateInteractions(sheetIndex, nextRow, aggregateStats.DecisionFactorSearchIndices,
            "DFSI", "[Decision Factors Search Index]");
        nextRow = AddAggregateInteractions(sheetIndex, nextRow, aggregateStats.AlternativesSearchIndices, 
            "ASI", "[Alternatives Search Index]");
        nextRow = AddAggregateInteractions(sheetIndex, nextRow, aggregateStats.DecisionFactorSelectionCounts, "DF Count", "(Repetition Counted)");
        nextRow = AddAggregateInteractions(sheetIndex, nextRow, aggregateStats.AlternativeSelectionCount, "Alt Count", "(Repetition Counted)");
        nextRow = AddAggregateInteraction(sheetIndex, nextRow, aggregateStats.SiDF, "SI DF");
        nextRow = AddAggregateInteraction(sheetIndex, nextRow, aggregateStats.SiAlt, "SI Alt");
        nextRow = AddAggregateInteraction(sheetIndex, nextRow, aggregateStats.Coverage, "Percentage of Unique Bins Reviewed");
        _excelCreator.AutoSizeColumn(sheetIndex, 1);
    }

    private int AddAggregateInteractions(int sheetIndex, int row, AggregateInteraction[] interactions, string title,
        string helpText = "")
    {
        var nextRow = row;
        _excelCreator.InsertString(sheetIndex, $"{title} {helpText} Mean", nextRow, 1);
        nextRow++;
        var enumeratedInteractions = interactions.Enumerate().ToArray();
        foreach (var (i, interaction) in enumeratedInteractions)
        {
            _excelCreator.InsertString(sheetIndex, $"{title} {i+1}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.Mean, nextRow, 2);
            nextRow++;
        }
        
        _excelCreator.InsertString(sheetIndex, $"{title} {helpText} S.D.", nextRow, 1);
        nextRow++;
        foreach (var (i, interaction) in enumeratedInteractions)
        {
            _excelCreator.InsertString(sheetIndex, $"{title} {i+1}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.StandardDeviation, nextRow, 2);
            nextRow++;
        }
        
        _excelCreator.InsertString(sheetIndex, $"{title} {helpText} Max", nextRow, 1);
        nextRow++;
        foreach (var (i, interaction) in enumeratedInteractions)
        {
            _excelCreator.InsertString(sheetIndex, $"{title} {i+1}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.Max, nextRow, 2);
            nextRow++;
        }
        
        _excelCreator.InsertString(sheetIndex, $"{title} {helpText} Min", nextRow, 1);
        nextRow++;
        foreach (var (i, interaction) in enumeratedInteractions)
        {
            _excelCreator.InsertString(sheetIndex, $"{title} {i+1}", nextRow, 1);
            _excelCreator.InsertNumber(sheetIndex, interaction.Min, nextRow, 2);
            nextRow++;
        }
        return nextRow;
    }
    
    private int AddAggregateInteraction(int sheetIndex, int row, AggregateInteraction interaction, string title)
    {
        var nextRow = row;
        _excelCreator.InsertString(sheetIndex, $"{title} Mean", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, interaction.Mean, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, $"{title} S.D.", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, interaction.StandardDeviation, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, $"{title} Max", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, interaction.Max, nextRow, 2);
        nextRow++;
        
        _excelCreator.InsertString(sheetIndex, $"{title} Min", nextRow, 1);
        _excelCreator.InsertNumber(sheetIndex, interaction.Min, nextRow, 2);

        return nextRow + 1;
    }

    private static AggregateInteractions AggregateStats(List<TracedInteractions> stats)
    {
        var common = stats[0];
        var alternativeSelectionPercentage = InitializeIntermediateAggregateInteractions(common.AlternativesSelectionPercentage.Length, stats.Count);
        var dimensionsSelectionPercentage = InitializeIntermediateAggregateInteractions(common.DecisionFactorsSelectionPercentage.Length, stats.Count);
        var totalBinsVisited = new IntermediateAggregateInteraction(stats.Count);
        var si = new IntermediateAggregateInteraction(stats.Count);
        var dimensionsSearchIndices = InitializeIntermediateAggregateInteractions(common.DecisionFactorsSearchIndices.Length, stats.Count);
        var alternativeSearchIndices = InitializeIntermediateAggregateInteractions(common.AlternativesSearchIndices.Length, stats.Count);
        var decisionStrategies = new Dictionary<string, int>();
        var attributeRanks = InitializeIntermediateAggregateInteractions(common.AttributeRanks.Length, stats.Count);
        var dimensionSelectionCounts = InitializeIntermediateAggregateInteractions(common.DecisionFactorSelectionCounts.Length, stats.Count);
        var totalDimensionSelectionCount = new IntermediateAggregateInteraction(stats.Count);
        var alternativeSelectionCount = InitializeIntermediateAggregateInteractions(common.AlternativeSelectionCount.Length, stats.Count);
        var totalAlternativeSelectionCount = new IntermediateAggregateInteraction(stats.Count);
        var siDim = new IntermediateAggregateInteraction(stats.Count);
        var siAlt = new IntermediateAggregateInteraction(stats.Count);
        var siMix = new IntermediateAggregateInteraction(stats.Count);
        var coverage = new IntermediateAggregateInteraction(stats.Count);

        foreach (var (i, interactions) in stats.Enumerate())
        {
            foreach(var (j, selectionPercentage) in interactions.AlternativesSelectionPercentage.Enumerate())
            {
                alternativeSelectionPercentage[j].AddValue(i, selectionPercentage);
            }
            
            foreach(var (j, selectionPercentage) in interactions.DecisionFactorsSelectionPercentage.Enumerate())
            {
                dimensionsSelectionPercentage[j].AddValue(i, selectionPercentage);
            }
            
            totalBinsVisited.AddValue(i, interactions.TotalBinsVisited);
            si.AddValue(i, interactions.Si);
            foreach(var (j, searchIndex) in interactions.DecisionFactorsSearchIndices.Enumerate())
            {
                dimensionsSearchIndices[j].AddValue(i, searchIndex);
            }

            foreach (var strategy in interactions.DecisionStrategies)
            {
                if (!decisionStrategies.TryAdd(strategy, 1))
                {
                    decisionStrategies[strategy]++;
                }
            }
            
            foreach(var (j, rank) in interactions.AttributeRanks.Enumerate())
            {
                attributeRanks[j].AddValue(i, rank);
            }
            
            foreach(var (j, count) in interactions.DecisionFactorSelectionCounts.Enumerate())
            {
                dimensionSelectionCounts[j].AddValue(i, count);
            }
            
            totalDimensionSelectionCount.AddValue(i, interactions.TotalDecisionFactorSelectionCount);
            foreach(var (j, count) in interactions.AlternativeSelectionCount.Enumerate())
            {
                alternativeSelectionCount[j].AddValue(i, count);
            }
            
            totalAlternativeSelectionCount.AddValue(i, interactions.TotalAlternativeSelectionCount);
            siDim.AddValue(i, interactions.SiDim);
            siAlt.AddValue(i, interactions.SiAlt);
            siMix.AddValue(i, interactions.SiMix);
            coverage.AddValue(i, interactions.Coverage);
        }
        
        return new AggregateInteractions
        {
            AlternativesSelectionPercentage = alternativeSelectionPercentage.Select(x => x.Aggregate()).ToArray(),
            DecisionFactorsSelectionPercentage = dimensionsSelectionPercentage.Select(x => x.Aggregate()).ToArray(),
            TotalBinsVisited = totalBinsVisited.Aggregate(),
            Si = si.Aggregate(),
            DecisionFactorSearchIndices = dimensionsSearchIndices.Select(x => x.Aggregate()).ToArray(),
            AlternativesSearchIndices = alternativeSearchIndices.Select(x => x.Aggregate()).ToArray(),
            DecisionStrategies = decisionStrategies,
            AttributeRanks = attributeRanks.Select(x => x.Aggregate()).ToArray(),
            DecisionFactorSelectionCounts = dimensionSelectionCounts.Select(x => x.Aggregate()).ToArray(),
            TotalDimensionSelectionCount = totalDimensionSelectionCount.Aggregate(),
            AlternativeSelectionCount = alternativeSelectionCount.Select(x => x.Aggregate()).ToArray(),
            TotalAlternativeSelectionCount = totalAlternativeSelectionCount.Aggregate(),
            SiDF = siDim.Aggregate(),
            SiAlt = siAlt.Aggregate(),
            SiMix = siMix.Aggregate(),
            Coverage = coverage.Aggregate()
        };
    }
    
    private static IntermediateAggregateInteraction[] InitializeIntermediateAggregateInteractions(int numElements, int numInteractions)
    {
        var interactions = new IntermediateAggregateInteraction[numElements];
        for (var i = 0; i < numElements; i++)
        {
            interactions[i] = new IntermediateAggregateInteraction(numInteractions);
        }
        return interactions;
    }
    
    private TracedInteractions AddUserStats(DecisionMatrixStatsData stats, int sheetIndex)
    {
        var tracedInteractions = StatsUtility.TraceInteractions(stats);
        TitledStat[] headers = 
            [
                new TitledStringStat("=== Metadata ===", ""),
                new TitledStringStat("Email", stats.ParticipantEmail),
                new TitledGuidStat("Matrix Guid", stats.ElementGuid),
                new TitledGuidStat("Stats Instance Guid", stats.Guid),
                new TitledDateTimeStat("Date", stats.StartTime),
                new TitledStringStat("=== Stats ===", ""),
                new TitledNumericStat("SI", tracedInteractions.Si),
                new TitledNumericStat("Dimension Factor SI", tracedInteractions.SiDim),
                new TitledNumericStat("Alternative SI", tracedInteractions.SiAlt),
                new TitledNumericStat("TTD", stats.ElapsedMilliseconds / 1000m),
                new TitledNumericStat("TTI (from first interaction to decision)", tracedInteractions.TimeToInteraction / 1000m),
                new TitledNumericStat("Final Choice", stats.Decision + 1),
                new TitledNumericStat("Total # of Unique Bins Reviewed (multiple selections not counted)", tracedInteractions.TotalBinsVisited),
                new TitledNumericStat("Total # of Bins Reviewed (includes multiple selections)", tracedInteractions.TotalDecisionFactorSelectionCount),
                new TitledNumericArrayStat("# of Decision Factor Bins Reviewed", tracedInteractions.DecisionFactorSelectionCounts),
                new TitledNumericArrayStat("Percentages of Decision Factor Bins Reviewed", tracedInteractions.DecisionFactorsSelectionPercentage),
                new TitledNumericArrayStat("# of Alternative Bins Reviewed", tracedInteractions.AlternativeSelectionCount),
                new TitledNumericArrayStat("Percentages of Alternative Bins Reviewed", tracedInteractions.AlternativesSelectionPercentage),
                (stats.RowRatings is not null 
                    ? new TitledNumericArrayStat("Alternative Ratings", stats.RowRatings) 
                    : new TitledStringStat("Alternative Ratings", "N/A")),
                new TitledNumericArrayStat("Decision Factors Search Indices", tracedInteractions.DecisionFactorsSearchIndices),
                new TitledNumericArrayStat("Alternatives Search Indices", tracedInteractions.AlternativesSearchIndices),
                new TitledNumericStat("Percentage of Unique Bins Reviewed", tracedInteractions.Coverage),
                new TitledStringStat("=== Stats Tracing ===", "")
            ];
        var numColumns = stats.ColumnCount;
        var nextRow = 1;
        foreach (var header in headers)
        {
            nextRow = InsertTitledStat(sheetIndex, header, nextRow, numColumns);
        }
        
        _excelCreator.InsertString(sheetIndex, "Decision portrait", nextRow, 1, style: "Large");
        _excelCreator.MergeCells(sheetIndex, $"A{nextRow}", $"A{nextRow + stats.RowCount - 1}");
        var interactions = tracedInteractions.InteractionMap;
        for (var row = 0; row < stats.RowCount; row++)
        {
            for (var col = 0; col < stats.ColumnCount; col++)
            {
                AddTracedInteraction(nextRow, col + 2, interactions[(row, col)], sheetIndex);
            }
            nextRow++;
        }
        
        _excelCreator.InsertString(sheetIndex, "=== Raw Interaction Stats ===", nextRow, 1, style: "Header");
        var columnLetter = ExcelCreator.DecimalToBase26(stats.ColumnCount + 1);
        _excelCreator.MergeCells(sheetIndex, $"A{nextRow}", $"{columnLetter}{nextRow}");
        nextRow++;
        for (var row = 0; row < stats.RowCount; row++)
        {
            for (var col = 0; col < stats.ColumnCount; col++)
            {
                AddDecisionMatrixStatsCell(nextRow, col + 2, stats[row, col], sheetIndex);
            }
            nextRow++;
        }
        
        return tracedInteractions;
    }

    private int InsertTitledStat(int sheetIndex, TitledStat header, int row, int numColumns)
    {
        var titleCell = _excelCreator.InsertString(sheetIndex, header.Title, row, 1);
        switch (header)
        {
            case TitledStringStat stringStat:
                if(stringStat.Value != "")
                {
                    _excelCreator.InsertString(sheetIndex, stringStat.Value, row, 2);
                }
                else
                {
                    var col = ExcelCreator.DecimalToBase26(numColumns + 1);
                    _excelCreator.MergeCells(sheetIndex, $"A{row}", $"{col}{row}");
                    titleCell.StyleIndex = _excelCreator.GetStyleIndex("Header");
                }
                return row + 1;
            case TitledGuidStat guidStat:
                _excelCreator.InsertGuid(sheetIndex, guidStat.Value, row, 2);
                return row + 1;
            case TitledNumericStat numericStat:
                _excelCreator.InsertNumber(sheetIndex, numericStat.Value, row, 2);
                return row + 1;
            case TitledNumericArrayStat numericArrayStat:
                foreach (var (i, value) in numericArrayStat.Values.Enumerate())
                {
                    _excelCreator.InsertNumber(sheetIndex, value, row, 2 + i);
                }
                return row + 1;
            case TitledDateTimeStat dateTimeStat:
                _excelCreator.InsertDateTime(sheetIndex, dateTimeStat.Value, row, 2);
                return row + 1;
            default:
                throw new ArgumentException("Invalid TitledStat type");
        }
    }

    private void AddDecisionMatrixStatsCell(int row, int col, DecisionMatrixStatsCellData cell, int sheetIndex)
    {
        if(row < 1 || col < 1)
        {
            throw new ArgumentException("Row and column numbers must be greater than 0");
        }

        var cellContents = "";
        cellContents += $"Interaction Count: {cell.Interactions.Count}\n";
        cellContents += $"Rating: {cell.Rating}\n";
        if (cell.VideoTracking is not null)
        {
            cellContents += $"Video Tracking: {{Start Times: {cell.VideoTracking.StartTimes.ToFormattedString()}, ";
            cellContents += $"End Times: {cell.VideoTracking.EndTimes.ToFormattedString()}}}\n";
        }
        else
        {
            cellContents += "Video Tracking: N/A\n";
        }
        
        if (cell.ImageTracking is not null)
        {
            cellContents += $"Image Tracking: {{Start Times: {cell.ImageTracking.StartTimes.ToFormattedString()}, ";
            cellContents += $"End Times: {cell.ImageTracking.EndTimes.ToFormattedString()}}}\n";
        }
        else
        {
            cellContents += "Image Tracking: N/A\n";
        }
        
        if (cell.AudioTracking is not null)
        {
            cellContents += $"Audio Tracking: {{Start Times: {cell.AudioTracking.StartTimes.ToFormattedString()}, ";
            cellContents += $"End Times: {cell.AudioTracking.EndTimes.ToFormattedString()}}}\n";
        }
        else
        {
            cellContents += "Audio Tracking: N/A\n";
        }
        
        if (cell.TextTracking is not null)
        {
            cellContents += $"Text Tracking: {{Start Times: {cell.TextTracking.StartTimes.ToFormattedString()}, ";
            cellContents += $"End Times: {cell.TextTracking.EndTimes.ToFormattedString()}}}\n";
        }
        else
        {
            cellContents += "Text Tracking: N/A\n";
        }
        
        _excelCreator.InsertString(sheetIndex, cellContents, row, col);
    }
    
    private void AddTracedInteraction(int row, int col, List<TracedInteraction> interactions, int sheetIndex)
    {
        if(row < 1 || col < 1)
        {
            throw new ArgumentException("Row and column numbers must be greater than 0");
        }

        var cellContents = interactions.Aggregate("",
            (current, interaction) =>
                current + $"({interaction.InteractionIndex + 1:D3}) {interaction.InteractionTime / 1000m:F4}\n");
        cellContents = cellContents[..^1];  // Remove trailing new line
        _excelCreator.InsertString(sheetIndex, cellContents, row, col, style: "Bordered");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _excelCreator.Dispose();
    }
}