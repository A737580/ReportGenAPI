using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportGen.Models;
public class Value
{
    public int Id { get; set; } 
    public DateTimeOffset StartDateTime { get; set; }
    [Required]
    [MaxLength(255)]
    [ForeignKey("Result")]
    public string FileName { get; set; } 
    public int ExecutionTimeS { get; set; }
    public decimal StoreValue { get; set; } // Поле для значения Value
    public Result Result { get; set; }
}