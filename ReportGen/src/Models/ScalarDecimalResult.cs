using System.ComponentModel.DataAnnotations.Schema;

namespace ReportGen.Models  
{
    public class ScalarDecimalResult
    {
        [Column("value")]
        public decimal Value { get; set; }
    }
}