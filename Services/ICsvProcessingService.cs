namespace ReportGen.Services;

public interface ICsvProcessingService
{
    Task ProcessCsvFileAsync(string fileName, Stream fileStream);
}
