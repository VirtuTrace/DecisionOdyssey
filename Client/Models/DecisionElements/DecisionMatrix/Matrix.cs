using System.IO.Compression;
using System.Text.Json;
using Common.Enums;

namespace Client.Models.DecisionElements.DecisionMatrix;

public class Matrix : DecisionElement
{
    public List<string> RowNames { get; } = [];
    public List<string> ColumnNames { get; } = [];
    public List<List<MatrixCell>> Data { get; } = [];
    public MatrixFeatures Features { get; set; }
    public int AllottedTime { get; set; } = -1;
    public MatrixCell Prompt { get; set; } = new();
    
    public int RowCount => RowNames.Count;
    public int ColumnCount => ColumnNames.Count;

    public MatrixCell this[int row, int col]
    {
        get => Data[row][col];
        set => Data[row][col] = value;
    }
    
    public string this[int index, bool row]
    {
        get => row ? RowNames[index] : ColumnNames[index];
        set
        {
            if (row)
            {
                RowNames[index] = value;
            }
            else
            {
                ColumnNames[index] = value;
            }
        }
    }
    
    public override void Reset()
    {
        base.Reset();
        
        RowNames.Clear();
        ColumnNames.Clear();
        Data.Clear();
        
        RowNames.Add("");
        ColumnNames.Add("");
        Data.Add([new MatrixCell()]);
    }
    
    public override (string, byte[]) Archive()
    {
        string matrixInfoJson;
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var matrixInfo = new DecisionMatrixMetadata
            {
                Name = Name,
                Guid = Guid,
                CreationTime = CreationTime,
                LastUpdated = DateTime.Now,
                Features = Features,
                RowNames = RowNames,
                ColumnNames = ColumnNames,
                AllottedTime = AllottedTime
            };
            matrixInfoJson = JsonSerializer.Serialize(matrixInfo);
            var zipEntry = archive.CreateEntry("metadata.json");
            using (var entryStream = zipEntry.Open())
            {
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write(matrixInfoJson);
                }
            }

            ArchiveData(archive, Prompt, "prompt");

            for (var row = 0; row < RowCount; row++)
            {
                for (var col = 0; col < ColumnCount; col++)
                {
                    ArchiveCell(archive, row, col);
                }
            }
        }

        memoryStream.Position = 0;
        return (matrixInfoJson, memoryStream.ToArray());
    }

    private void ArchiveCell(ZipArchive archive, int row, int col)
    {
        var cell = this[row, col];
        ArchiveData(archive, cell, $"entry_{row}_{col}");
    }
    
    public void AddRow(int index = -1)
    {
        var row = new List<MatrixCell>();
        for(var i = 0; i < ColumnCount; i++)
        {
            row.Add(new MatrixCell());
        }
        
        if(index == -1)
        {
            RowNames.Add("");
            Data.Add(row);
        }
        else
        {
            RowNames.Insert(index, "");
            Data.Insert(index, row);
        }
    }

    public void AddColumn(int index = -1)
    {
        if (index == -1)
        {
            ColumnNames.Add("");
            foreach (var row in Data)
            {
                row.Add(new MatrixCell());
            }
        }
        else
        {
            ColumnNames.Insert(index, "");
            foreach (var row in Data)
            {
                row.Insert(index, new MatrixCell());
            }
        }
    }

    public void RemoveRow(int index = -1)
    {
        var removeIndex = index == -1 ? RowCount - 1 : index;
        RowNames.RemoveAt(removeIndex);
        Data.RemoveAt(removeIndex);
    }
    
    public void RemoveColumn(int index = -1)
    {
        var removeIndex = index == -1 ? ColumnCount - 1 : index;
        ColumnNames.RemoveAt(removeIndex);
        foreach (var row in Data)
        {
            row.RemoveAt(removeIndex);
        }
    }

    public void FromMetadata(DecisionMatrixMetadata metadata)
    {
        Name = metadata.Name;
        Guid = metadata.Guid;
        CreationTime = metadata.CreationTime;
        Features = metadata.Features;
        RowNames.Clear();
        RowNames.AddRange(metadata.RowNames);
        ColumnNames.Clear();
        ColumnNames.AddRange(metadata.ColumnNames);
        AllottedTime = metadata.AllottedTime;

        Data.Clear();
        foreach (var row in RowNames.Select(_ => ColumnNames.Select(_ => new MatrixCell()).ToList()))
        {
            Data.Add(row);
        }
    }
}