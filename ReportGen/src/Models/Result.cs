
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportGen.Models;

public class Result
{
    [Key]
    [MaxLength(255)]
    public string FileName { get; set; }
    public int DeltaTime { get; set; }
    public DateTimeOffset MinimumDateTime { get; set; }
    public double AvgExecutionTime { get; set; }
    public decimal AvgStoreValue { get; set; }
    public decimal MedianStoreValue { get; set; }
    public decimal MaximumStoreValue { get; set; }
    public decimal MinimumStoreValue { get; set; }
    public ICollection<Value> Values { get; set; } = new List<Value>();
}