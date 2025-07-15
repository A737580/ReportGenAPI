
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
    public DateTime MinimumDateTime { get; set; }
    [Column(TypeName = "decimal(18, 4)")]
    public decimal AvgExecutionTime { get; set; }
    [Column(TypeName = "decimal(18, 4)")]
    public decimal AvgStoreValue { get; set; }
    [Column(TypeName = "decimal(18, 4)")]
    public decimal MedianStoreValue { get; set; }
    [Column(TypeName = "decimal(18, 4)")]
    public decimal MaximumStoreValue { get; set; }
    [Column(TypeName = "decimal(18, 4)")]
    public decimal MinimumStoreValue { get; set; }
    public ICollection<Value> Values { get; set; } = new List<Value>();
}