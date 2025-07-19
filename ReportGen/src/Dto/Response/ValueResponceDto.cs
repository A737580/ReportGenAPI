namespace ReportGen.Models;
public class ValueResponceDto
{
    public int Id { get; set; } 
    public DateTimeOffset StartDateTime { get; set; }
    public string FileName { get; set; } 
    public int ExecutionTimeS { get; set; }
    public decimal StoreValue { get; set; }
}