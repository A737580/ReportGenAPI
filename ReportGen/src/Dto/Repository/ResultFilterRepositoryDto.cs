using System;

namespace ReportGen.Models
{
    public class ResultFilterRepositoryDto
    {
        public string? FileName { get; set; }
        public DateTimeOffset? MinMinimumDateTime { get; set; }
        public DateTimeOffset? MaxMinimumDateTime { get; set; }
        public double? MinAvgExecutionTime { get; set; }
        public double? MaxAvgExecutionTime { get; set; }
        public decimal? MinAvgStoreValue { get; set; }
        public decimal? MaxAvgStoreValue { get; set; }
        public int? MinDeltaTimeS { get; set; }
        public int? MaxDeltaTimeS { get; set; }
    }
}