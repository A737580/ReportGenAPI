namespace ReportGen.Models;
public class CsvValidationError
{
    public int RowNumber { get; set; } 
    public string ColumnName { get; set; } 
    public string Value { get; set; } 
    public string Message { get; set; } 
}