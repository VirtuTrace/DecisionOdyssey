using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

namespace Client.Excel;

public class ExcelCreator : IDisposable
{
    private readonly MemoryStream _memoryStream;
    private readonly SpreadsheetDocument _document;
    private readonly WorkbookPart _workbookPart;
    private readonly Sheets _sheets;
    private readonly SharedStringTablePart _sharedStringTablePart;
    private readonly List<WorksheetPart> _worksheetParts = [];
    private readonly OpenXmlValidator _xmlValidator = new(FileFormatVersions.Microsoft365);

    private readonly Dictionary<string, uint> _styleLookup = new()
    {
        ["Default"] = 0,
        ["Large"] = 1,
        ["Bordered"] = 2,
        ["Header"] = 3,
        ["DateTime"] = 4
    };
    
    private int _numSheets = 1;
    private bool _disposed;

    public ExcelCreator(string sheet1Name = "Sheet1")
    {
        _memoryStream = new MemoryStream();
        _document = SpreadsheetDocument.Create(_memoryStream, SpreadsheetDocumentType.Workbook);
        _workbookPart = _document.AddWorkbookPart();
        _workbookPart.Workbook = new Workbook();
        
        _sharedStringTablePart = _workbookPart.AddNewPart<SharedStringTablePart>();
        _sharedStringTablePart.SharedStringTable = new SharedStringTable();
        
        var worksheetPart = _workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = CreateWorksheet();
        
        _sheets = _workbookPart.Workbook.AppendChild(new Sheets());
        
        _sheets.AppendChild(new Sheet
        {
            Id = _workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = sheet1Name
        });
        
        _worksheetParts.Add(worksheetPart);
        
        ConfigureStylesheet();
        
        _workbookPart.Workbook.Save();
    }
    
    public uint GetStyleIndex(string style) => _styleLookup[style];
    
    public int AddSheet(string name = "")
    {
        var worksheetPart = _workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = CreateWorksheet();
        
        var index = _numSheets++;
        if (name == "")
        {
            name = "Sheet" + _numSheets;
        }
        else
        {
            name = name.Length > 31 ? name[..31] : name;
        }
        
        var sheet = new Sheet
        {
            Id = _workbookPart.GetIdOfPart(worksheetPart),
            SheetId = (uint)_numSheets,
            Name = name
        };
        
        _worksheetParts.Add(worksheetPart);
        _sheets.Append(sheet);
        
        _workbookPart.Workbook.Save();
        
        return index;
    }

    public Cell InsertString(int worksheetIndex, string text, int rowIndex, int columnIndex, string? style = null)
    {
        var index = InsertSharedStringItem(text);
        var (worksheetPart, cell) = GetWorksheetCell(worksheetIndex, rowIndex, columnIndex);

        cell.CellValue = new CellValue(index.ToString());
        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
        if (style is not null)
        {
            cell.StyleIndex = _styleLookup[style];
        }

        worksheetPart.Worksheet.Save();
        
        return cell;
    }
    
    public void InsertHeader(int worksheetIndex, string text, int rowIndex, int numColumns)
    {
        InsertString(worksheetIndex, text, rowIndex, 1, "Header");
        MergeCells(worksheetIndex, "A" + rowIndex, DecimalToBase26(numColumns) + rowIndex);
    }
    
    public void InsertNumber(int worksheetIndex, decimal number, int rowIndex, int columnIndex)
    {
        var (worksheetPart, cell) = GetWorksheetCell(worksheetIndex, rowIndex, columnIndex);

        cell.CellValue = new CellValue(number);
        cell.DataType = new EnumValue<CellValues>(CellValues.Number);

        worksheetPart.Worksheet.Save();
    }

    public void InsertNumbers(int worksheetIndex, int[] numbers, int rowIndex, int columnIndex)
    {
        for (var i = 0; i < numbers.Length; i++)
        {
            InsertNumber(worksheetIndex, numbers[i], rowIndex, columnIndex + i);
        }
    }
    
    public void InsertNumbers(int worksheetIndex, decimal[] numbers, int rowIndex, int columnIndex)
    {
        for (var i = 0; i < numbers.Length; i++)
        {
            InsertNumber(worksheetIndex, numbers[i], rowIndex, columnIndex + i);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InsertGuid(int worksheetIndex, Guid guid, int rowIndex, int columnIndex)
    {
        InsertString(worksheetIndex, guid.ToString(), rowIndex, columnIndex);
    }
    
    public void InsertDateTime(int worksheetIndex, DateTime dateTime, int rowIndex, int columnIndex)
    {
        var (worksheetPart, cell) = GetWorksheetCell(worksheetIndex, rowIndex, columnIndex);

        cell.CellValue = new CellValue(dateTime.ToOADate());
        //cell.DataType = new EnumValue<CellValues>(CellValues.Date);
        cell.StyleIndex = _styleLookup["DateTime"];

        worksheetPart.Worksheet.Save();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (WorksheetPart, Cell) GetWorksheetCell(int worksheetIndex, int rowIndex, int columnIndex)
    {
        var worksheetPart = _worksheetParts[worksheetIndex];
        var cell = InsertCellInWorksheet(rowIndex, columnIndex, worksheetPart);
        return (worksheetPart, cell);
    }
    
    /// <summary>
    ///     Save the Excel file to a MemoryStream and return it. The document write is disposed after this method is called.
    /// </summary>
    /// <returns>MemoryStream containing the Excel file</returns>
    public MemoryStream Save()
    {
        #if DEBUG
        var errors = _xmlValidator.Validate(_document);
        foreach (var error in errors)
        {
            Console.WriteLine($"""
                               ================================
                               Id: {error.Id}
                               Description: {error.Description}
                               Path: {error.Path?.PartUri}
                               Part: {error.Part?.Uri}
                               Node: {error.Node?.LocalName}
                               Type: {error.ErrorType}
                               ================================
                               """);
        }
        #endif
        
        _document.Dispose();
        _memoryStream.Position = 0;
        return _memoryStream;
    }

    public void MergeCells(int worksheetIndex, string startCell, string endCell)
    {
        var worksheetPart = _worksheetParts[worksheetIndex];
        var worksheet = worksheetPart.Worksheet;
        var mergeCells = worksheet.Elements<MergeCells>().FirstOrDefault();
        if (mergeCells is null)
        {
            mergeCells = new MergeCells();
            worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetData>().FirstOrDefault());
        }
        mergeCells.Append(new MergeCell { Reference = new StringValue($"{startCell}:{endCell}") });
    }

    public void AutoSizeColumn(int worksheetIndex, int columnIndex)
    {
        var worksheetPart = _worksheetParts[worksheetIndex];
        var worksheet = worksheetPart.Worksheet;
        var sheetData = worksheet.GetFirstChild<SheetData>()!;
        var columns = worksheet.GetFirstChild<Columns>();
        if (columns is null)
        {
            columns = new Columns();
            worksheet.InsertBefore(columns, sheetData);
        }
        CalculateColumnWidths(columns, sheetData, columnIndex);
        worksheet.Save();
    }
    
    private void CalculateColumnWidths(Columns columns, SheetData sheetData, int columnIndex)
    {
        var columnString = DecimalToBase26(columnIndex);
        
        foreach (var row in sheetData.Elements<Row>())
        {
            foreach (var cell in row.Elements<Cell>())
            {
                if (!cell.CellReference?.Value?.StartsWith(columnString) ?? true)
                {
                    continue;
                }
                
                var cellValue = GetCellValue(cell);
                var cellLength = cellValue.Length;
                var column = columns.Elements<Column>().FirstOrDefault(c => MatchColumn(c, columnIndex));
                if (column is null)
                {
                    column = new Column
                    {
                        Min = (uint)columnIndex,
                        Max = (uint)columnIndex,
                        CustomWidth = true,
                        BestFit = true
                    };
                    columns.Append(column);
                }

                if (column.Width is null || cellLength > column.Width)
                {
                    column.Width = (double)cellLength;
                }
                
                break;
            }
        }
    }

    private static bool MatchColumn(Column column, int columnIndex)
    {
        return column.Min is not null && column.Max is not null && column.Min == columnIndex && column.Max == columnIndex;
    }

    private string GetCellValue(Cell cell)
    {
        if (cell.DataType is null)
        {
            throw new Exception("Cell does not have a data type");
        }
        
        if (cell.DataType == CellValues.SharedString)
        {
            return GetSharedStringItem(cell);
        }

        if (cell.DataType == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? "";
        }

        if (cell.DataType == CellValues.Date)
        {
            var date = DateTime.FromOADate(double.Parse(cell.CellValue!.Text));
            return date.ToString("m/d/yy h:mm tt");
        }
        
        return cell.InnerText;
    }
    
    private int InsertSharedStringItem(string text)
    {
        var i = 0;
        // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
        foreach (var item in _sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>())
        {
            if (item.InnerText == text)
            {
                return i;
            }

            i++;
        }

        // The text does not exist in the part. Create the SharedStringItem and return its index.
        _sharedStringTablePart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
        _sharedStringTablePart.SharedStringTable.Save();

        return i;
    }

    private string GetSharedStringItem(Cell cell)
    {
        var stringTable = _sharedStringTablePart.SharedStringTable;
        var index = int.Parse(cell.CellValue!.Text);
        return stringTable.ChildElements[index].InnerText;
    }
    
    // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
    // If the cell already exists, returns it. 
    private static Cell InsertCellInWorksheet(int rowIndex, int columnIndex, WorksheetPart worksheetPart)
    {
        var worksheet = worksheetPart.Worksheet;
        // Safety: SheetData is guaranteed to exist because whenever we create a worksheet, we add a SheetData to it.
        var sheetData = worksheet.GetFirstChild<SheetData>()!;
        var cellReference = DecimalToBase26(columnIndex) + rowIndex;

        // If the worksheet does not contain a row with the specified row index, insert one.
        var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex is not null && r.RowIndex == rowIndex);

        if (row is null)
        {
            row = new Row { RowIndex = (uint)rowIndex };
            sheetData.Append(row);
        }

        var targetCell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference is not null && c.CellReference.Value == cellReference);
        if (targetCell is not null)
        {
            return targetCell;
        }
        
        // If there is not a cell with the specified column name, insert one.  
        // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
        Cell? refCell = null;

        // FIXME: Not sure if the loop is guaranteed to break, so refCell might be null.
        foreach (var cell in row.Elements<Cell>())
        {
            if (string.Compare(cell.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase) > 0)
            {
                refCell = cell;
                break;
            }
        }

        var newCell = new Cell { CellReference = cellReference };
        row.InsertBefore(newCell, refCell);

        worksheet.Save();
        return newCell;
    }

    private void ConfigureStylesheet()
    {
        var stylesPart = _workbookPart.AddNewPart<WorkbookStylesPart>();
        var stylesheet = new Stylesheet();
        
        var dateTimeNumberFormat = new NumberingFormat
        {
            NumberFormatId = 165,
            FormatCode = @"[$-409]m/d/yy\ h:mm\ AM/PM;@"
        };
        
        var numberFormats = new NumberingFormats();
        numberFormats.Append(dateTimeNumberFormat);
        
        var font0 = new Font
        {
            FontName = new FontName
            {
                Val = "Calibri"
            },
            FontSize = new FontSize
            {
                Val = 11
            }
        };
        
        var font1 = new Font
        {
            FontName = new FontName
            {
                Val = "Calibri"
            },
            FontSize = new FontSize
            {
                Val = 16
            },
        };
        
        var fonts = new Fonts();
        fonts.Append(font0);
        fonts.Append(font1);

        var border0 = new Border();
        
        var border1 = new Border
        {
            BottomBorder = new BottomBorder
            {
                Color = new Color
                {
                    Indexed = 64
                },
                Style = BorderStyleValues.Thin
            },
            TopBorder = new TopBorder
            {
                Color = new Color
                {
                    Indexed = 64
                },
                Style = BorderStyleValues.Thin
            },
            LeftBorder = new LeftBorder
            {
                Color = new Color
                {
                    Indexed = 64
                },
                Style = BorderStyleValues.Thin
            },
            RightBorder = new RightBorder
            {
                Color = new Color
                {
                    Indexed = 64
                },
                Style = BorderStyleValues.Thin
            }
        };
        
        var borders = new Borders();
        borders.Append(border0);
        borders.Append(border1);

        var fillNone = new Fill
        {
            PatternFill = new PatternFill
            {
                PatternType = PatternValues.None
            }
        };
        
        var fills = new Fills();
        fills.Append(fillNone);
        
        var format0 = new CellFormat
        {
            FormatId = 0,
            FontId = 0,
            BorderId = 0,
            FillId = 0
        };
            
        var cellStyleFormat = new CellStyleFormats();
        cellStyleFormat.Append(format0);
        
        var cellStyle = new CellStyle
        {
            Name = "Normal",
            FormatId = 0,
            BuiltinId = 0
        };
        
        var cellStyles = new CellStyles();
        cellStyles.Append(cellStyle);
        
        var cellFormats = new CellFormats();
        
        var defaultCellFormat = new CellFormat
        {
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
        };
        
        var largeCellFormat = new CellFormat
        {
            FontId = 1,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            Alignment = new Alignment
            {
                Horizontal = HorizontalAlignmentValues.Right,
                Vertical = VerticalAlignmentValues.Center
            }
        };
        
        var borderedCellFormat = new CellFormat
        {
            FontId = 0,
            FillId = 0,
            BorderId = 1,
            FormatId = 0,
            ApplyBorder = true
        };
        
        var headerCellFormat = new CellFormat
        {
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            Alignment = new Alignment
            {
                Horizontal = HorizontalAlignmentValues.Center,
            }
        };
        
        var dateTimeCellFormat = new CellFormat
        {
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            NumberFormatId = dateTimeNumberFormat.NumberFormatId,
            ApplyNumberFormat = true
        };
        
        cellFormats.Append(defaultCellFormat);
        cellFormats.Append(largeCellFormat);
        cellFormats.Append(borderedCellFormat);
        cellFormats.Append(headerCellFormat);
        cellFormats.Append(dateTimeCellFormat);
        
        stylesheet.Append(numberFormats);
        stylesheet.Append(fonts);
        stylesheet.Append(fills);
        stylesheet.Append(borders);
        stylesheet.Append(cellStyleFormat);
        stylesheet.Append(cellFormats);
        stylesheet.Append(cellStyles);
        
        // Adding namespaces to the Stylesheet
        stylesheet.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
        stylesheet.AddNamespaceDeclaration("x14ac", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac");
        stylesheet.AddNamespaceDeclaration("x16r2", "http://schemas.microsoft.com/office/spreadsheetml/2015/02/main");
        stylesheet.AddNamespaceDeclaration("xr", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision");
        
        // Set the Ignorable attribute to specify which prefixes should be ignored by older readers
        stylesheet.MCAttributes = new MarkupCompatibilityAttributes
        {
            Ignorable = "x14ac x16r2 xr"
        };
        
        stylesPart.Stylesheet = stylesheet;
        stylesPart.Stylesheet.Save();
        var errors = _xmlValidator.Validate(stylesheet);
        foreach (var error in errors)
        {
            Console.WriteLine($"Id: {error.Id}\nDescription: {error.Description}\nPath: {error.Path}\nPart: {error.Part}\nNode: {error.Node}\n\n{error}");
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Worksheet CreateWorksheet() => new(new SheetData());
    
    public static string DecimalToBase26(int number)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Number must be greater than 0");
        }

        var result = "";
        while (number > 0)
        {
            number--;  // Adjust for 0-based indexing of letters
            var remainder = number % 26;
            var letter = (char)('A' + remainder);
            result = letter + result;
            number /= 26;
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        GC.SuppressFinalize(this);
        //_memoryStream.Dispose();
        _document.Dispose();
        _disposed = true;
    }
}
