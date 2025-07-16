namespace ReportGen.Models;
public class CsvValidationException : Exception
{
    public List<CsvValidationError> Errors { get; }

    public CsvValidationException(string message, List<CsvValidationError> errors) : base(message)
    {
        Errors = errors ?? new List<CsvValidationError>();
    }
}