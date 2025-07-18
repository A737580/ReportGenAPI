using System;

namespace ReportGen.Models
{
    public class ResultResponceDto
    {
    public string FileName { get; set; }
    public int DeltaTimeS { get; set; }
    public DateTimeOffset MinimumDateTime { get; set; }
    public double AvgExecutionTime { get; set; }
    public decimal AvgStoreValue { get; set; }
    public decimal MedianStoreValue { get; set; }
    public decimal MaximumStoreValue { get; set; }
    public decimal MinimumStoreValue { get; set; }
    }
}