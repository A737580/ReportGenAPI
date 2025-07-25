

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Models;

namespace ReportGen.Services;


public class CsvProcessingService : ICsvProcessingService
{
    private readonly ReportGenDbContext _context;

    public CsvProcessingService(ReportGenDbContext context)
    {
        _context = context;
    }

    public async Task ProcessCsvFileAsync(string fileName, Stream fileStream)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        List<CsvValidationError> validationErrors = new List<CsvValidationError>();
        List<Value> valuesToInsert = new List<Value>();
        int rowNumber = 0;
        bool hasTitle = false;

        try
        {
            await _context.Values.Where(v => v.FileName == fileName).ExecuteDeleteAsync();
            await _context.Results.Where(r => r.FileName == fileName).ExecuteDeleteAsync();

            var resultEntity = await _context.Results.FindAsync(fileName);

            if (resultEntity == null)
            {
                resultEntity = new Result
                {
                    FileName = fileName,
                    DeltaTimeS = 0,
                    MinimumDateTime = DateTimeOffset.MinValue,
                    AvgExecutionTime = 0,
                    AvgStoreValue = 0,
                    MedianStoreValue = 0,
                    MaximumStoreValue = 0,
                    MinimumStoreValue = 0
                };
                await _context.Results.AddAsync(resultEntity);

                await _context.SaveChangesAsync();
            }


            using (var reader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {

                    rowNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split(';');

                    if (parts.Length != 3)
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            Message = $"Некорректное количество колонок. Ожидается 3, найдено {parts.Length}.",
                            Value = line
                        });
                        continue;
                    }

                    if (!hasTitle)
                    {
                        if (parts[0].Trim().ToLower() != "date")
                        {
                            continue;
                        }
                        hasTitle = string.Join("", parts.Select(x => x.Trim())).ToLower().Trim() == "dateexecutiontimevalue";
                        continue;
                    }

                    DateTimeOffset StartDateTime;
                    int executionTimeS;
                    decimal storeValue;

                    if (!DateTimeOffset.TryParseExact(parts[0].Trim(), "yyyy-MM-ddTHH-mm-ss.ffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out StartDateTime))
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Date",
                            Value = parts[0],
                            Message = "Неверный формат даты. Пример формата даты: 2023-01-01T00-00-00.0000Z или 2024-07-16T16-30-00.1234Z"
                        });
                    }
                    else if (!IsDateInDateRange(StartDateTime, new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeOffset.Now))
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Date",
                            Value = parts[0],
                            Message = "Дата не может быть позже текущей и раньше 01.01.2000."
                        });
                    }

                    if (!int.TryParse(parts[1].Trim(), out executionTimeS))
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "ExecutionTime",
                            Value = parts[1],
                            Message = "Неверный формат времени выполнения, ожидается целое число."
                        });
                    }
                    else if (executionTimeS < 0)
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "ExecutionTime",
                            Value = parts[1],
                            Message = "Время выполнения не может быть отрицательным."
                        });
                    }

                    if (!decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out storeValue))
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Value",
                            Value = parts[2],
                            Message = "Неверный формат значения Store, ожидается число с плавающей запятой."
                        });
                    }

                    else if (storeValue < 0)
                    {
                        validationErrors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Value",
                            Value = parts[2],
                            Message = "Знак показателя не может быть меньше нуля."
                        });
                    }

                    if (validationErrors.Any(e => e.RowNumber == rowNumber))
                    {
                        continue;
                    }

                    valuesToInsert.Add(new Value
                    {
                        FileName = fileName,
                        StartDateTime = StartDateTime,
                        ExecutionTimeS = executionTimeS,
                        StoreValue = storeValue
                    });

                }
            }

            if (!hasTitle)
            {
                throw new InvalidOperationException("CSV-файл имеет неверный формат, должен быть заголовок - Date;ExecutionTime;Value");

            }

            if (validationErrors.Any())
            {
                throw new CsvValidationException($"Обнаружены ошибки при валидации CSV-файла: {fileName}.", validationErrors);

            }

            if (valuesToInsert.Count < 1 || valuesToInsert.Count > 10000)
            {
                throw new InvalidOperationException("CSV-файл не может иметь менее 1 или более 10000 записей.");

            }

            await _context.AddRangeAsync(valuesToInsert);
            await _context.SaveChangesAsync();

            await CalculateAndUpsertResultsAsync(fileName);

            await transaction.CommitAsync();

            
        }
        catch (CsvValidationException ex)
        {

            await transaction.RollbackAsync();

            throw new CsvValidationException(ex.Message, validationErrors);

        }
        catch (InvalidOperationException ex)
        {

            await transaction.RollbackAsync();

            throw new InvalidOperationException(ex.Message);

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Произошла ошибка при обработке файла'{fileName}'.", ex);
        }
    }
    private bool IsDateInDateRange(DateTimeOffset targetDate, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return targetDate >= startDate && targetDate <= endDate;
    }
    private async Task CalculateAndUpsertResultsAsync(string fileName)
    {
        var query = await _context.Values
            .Where(v => v.FileName == fileName)
            .GroupBy(v => v.FileName)
            .Select(g => new 
            {
                FileName = g.Key,
                DeltaTime = (int)(g.Max(v => v.StartDateTime)-g.Min(v => v.StartDateTime)).TotalSeconds,
                MinimumDateTime = g.Min(v => v.StartDateTime),
                AvgExecutionTime = g.Average(v => v.ExecutionTimeS),
                AvgStoreValue = g.Average(v => v.StoreValue),
                MaximumStoreValue = g.Max(v => v.StoreValue),
                MinimumStoreValue = g.Min(v => v.StoreValue), 
            })
            .FirstOrDefaultAsync();

        decimal medianStoreValue = 0m;

        if (query != null)
        {
            try
            {
                medianStoreValue = await _context.Set<ScalarDecimalResult>()
                                        .FromSqlInterpolated($"SELECT get_median_store_value_for_file({fileName}) AS value")
                                        .AsNoTracking()
                                        .Select(r => r.Value)
                                        .OrderBy(x => 1)
                                        .FirstAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Произошла ошибка при вычислении медианы.{ex.Message}");
            }

            var existingResult = await _context.Results.FindAsync(fileName);

            if (existingResult != null)
            {
                existingResult.FileName = query.FileName; 
                existingResult.DeltaTimeS = query.DeltaTime; 
                existingResult.MinimumDateTime = query.MinimumDateTime;
                existingResult.AvgExecutionTime = query.AvgExecutionTime;
                existingResult.AvgStoreValue = query.AvgStoreValue;
                existingResult.MedianStoreValue = medianStoreValue;
                existingResult.MaximumStoreValue = query.MaximumStoreValue;
                existingResult.MinimumStoreValue = query.MinimumStoreValue;

                await _context.SaveChangesAsync();
            }
            else
            {

                throw new InvalidOperationException($"Ошибка обработки.");
            }
        }
    }
}

