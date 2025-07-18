using System;

namespace ReportGen.Models
{
    public class ResultFilterParametersDto
    {
        public string? FileName { get; set; }
        public string? MinMinimumDateTime { get; set; }
        public string? MaxMinimumDateTime { get; set; }
        public double? MinAvgExecutionTime { get; set; }
        public double? MaxAvgExecutionTime { get; set; }
        public decimal? MinAvgStoreValue { get; set; }
        public decimal? MaxAvgStoreValue { get; set; }
        public int? MinDeltaTimeS { get; set; }
        public int? MaxDeltaTimeS { get; set; }
    }
}