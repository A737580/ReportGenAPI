

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Models;

namespace ReportGen.Services;


public class CsvProcessingService : ICsvProcessingService
{
    private readonly ReportGenDbContext _dbContext;

    public CsvProcessingService(ReportGenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ProcessCsvFileAsync(string fileName, Stream fileStream)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        List<CsvValidationError> validationErrors = new List<CsvValidationError>();
        List<Value> valuesToInsert = new List<Value>();
        int rowNumber = 0;
        bool hasTitle = false;

        try
        {
            await _dbContext.Values.Where(v => v.FileName == fileName).ExecuteDeleteAsync();
            await _dbContext.Results.Where(r => r.FileName == fileName).ExecuteDeleteAsync();

            using (var reader = new StreamReader(fileStream))
            {
                string line;
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

            if (valuesToInsert.Count < 1 || valuesToInsert.Count > 10000)
            {
                throw new InvalidOperationException("CSV-файл не может иметь менее 1 или более 10000 записей.");
            }

            if (validationErrors.Any())
            {
                throw new CsvValidationException("Обнаружены ошибки при валидации CSV-файла.", validationErrors);
            }


            await _dbContext.AddRangeAsync(valuesToInsert);
            await _dbContext.SaveChangesAsync();

            await CalculateAndUpsertResultsAsync(fileName);

            await transaction.CommitAsync();
        }
        catch (CsvValidationException) 
        {
            await transaction.RollbackAsync();
            throw; 
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Произошла ошибка при обработке файла '{fileName}'.", ex);
        }
    }
    private bool IsDateInDateRange(DateTimeOffset targetDate, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return targetDate >= startDate && targetDate <= endDate;
    }
    private async Task CalculateAndUpsertResultsAsync(string fileName)
    {
        var query = await _dbContext.Values
            .Where(v => v.FileName == fileName)
            .GroupBy(v => v.FileName)
            .Select(g => new
            {
                FileName = g.Key,
                DeltaTime = g.Max(v => v.StartDateTime)-g.Min(v => v.StartDateTime),
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
                medianStoreValue = await _dbContext.Set<ScalarDecimalResult>()
                                        .FromSqlInterpolated($"SELECT get_median_store_value_for_file({fileName})")
                                        .AsNoTracking()
                                        .Select(r => r.Value)
                                        .FirstAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при вычислении медианы для файла {fileName}.");
            }
            var result = new Result
            {
                FileName = query.FileName,
                DeltaTime = (int)query.DeltaTime.TotalSeconds,
                MinimumDateTime = query.MinimumDateTime,
                AvgExecutionTime = query.AvgExecutionTime, 
                AvgStoreValue = query.AvgStoreValue,
                MedianStoreValue = medianStoreValue,
                MaximumStoreValue = query.MaximumStoreValue,
                MinimumStoreValue = query.MinimumStoreValue
            };

            var existingResult = await _dbContext.Results.FindAsync(fileName);
            if (existingResult == null)
            {
                await _dbContext.Results.AddAsync(result);
            }
            else
            {
                _dbContext.Entry(existingResult).CurrentValues.SetValues(result);
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}

