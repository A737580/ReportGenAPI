
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ReportGen.Interfaces;
using ReportGen.Models;
using ReportGen.Services;

namespace ReportGen.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IResultRepository _resultRepository;
    private readonly ICsvProcessingService _csvProcessingService;

    public ReportController(IResultRepository resultRepository, ICsvProcessingService csvProcessingService)
    {
        _resultRepository = resultRepository;
        _csvProcessingService = csvProcessingService;
    }

    [HttpPost("upload_csv")]
    public async Task<IActionResult> UploadCsvFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Загрузите непустой файл.");
        }

        string fileName = file.FileName;

        if (Path.GetExtension(fileName).ToLower() != ".csv")
        {
            return BadRequest("Разрешены только файлы с расширением .csv");
        }

        await _csvProcessingService.ProcessCsvFileAsync(fileName, file.OpenReadStream());
        return Ok($"Файл '{fileName}' успешно загружен и обработан.");

    }

    [HttpPost("search_results")]
    public async Task<ActionResult<IEnumerable<Result>>> GetResultsByParameters([FromBody]ResultFilterParametersDto requestParameters)
    {
        if (requestParameters == null)
        {
            return BadRequest("Параметры фильтрации не могут быть пустыми.");
        }

        string dateFormat = "yyyy-MM-ddTHH-mm-ss.ffffZ";
        var cultureInfo = CultureInfo.InvariantCulture;
        var dateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        DateTimeOffset minParsedValue = DateTimeOffset.Now;
        DateTimeOffset maxParsedValue = DateTimeOffset.Now;


        if (!string.IsNullOrEmpty(requestParameters.MinMinimumDateTime))
        {
            if (!DateTimeOffset.TryParseExact(requestParameters.MinMinimumDateTime, dateFormat, cultureInfo, dateTimeStyles, out minParsedValue))
            {
                ModelState.AddModelError(nameof(requestParameters.MinMinimumDateTime), "Неверный формат даты. Пример формата даты: 2023-01-01T00-00-00.0000Z или 2024-07-16T16-30-00.1234Z.");
            }
        }

        if (!string.IsNullOrEmpty(requestParameters.MaxMinimumDateTime))
        {
            if (!DateTimeOffset.TryParseExact(requestParameters.MaxMinimumDateTime, dateFormat, cultureInfo, dateTimeStyles, out maxParsedValue))
            {
                ModelState.AddModelError(nameof(requestParameters.MinMinimumDateTime), "Неверный формат даты. Пример формата даты: 2023-01-01T00-00-00.0000Z или 2024-07-16T16-30-00.1234Z.");
            }
        }

        var repositoryParameters = new ResultFilterRepositoryDto()
        { 
            FileName = requestParameters.FileName,
            MinMinimumDateTime = minParsedValue,
            MaxMinimumDateTime = maxParsedValue,
            MinAvgExecutionTime = requestParameters.MinAvgExecutionTime,
            MaxAvgExecutionTime = requestParameters.MaxAvgExecutionTime,
            MinAvgStoreValue = requestParameters.MinAvgStoreValue,
            MaxAvgStoreValue = requestParameters.MaxAvgStoreValue,
            MinDeltaTimeS = requestParameters.MinDeltaTimeS,
            MaxDeltaTimeS = requestParameters.MaxDeltaTimeS

        };

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var results = await _resultRepository.GetResultsByParametersAsync(repositoryParameters);
        return Ok(results);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Value>>> GetLatestValues()
    {
        var result = await _resultRepository.GetLatestValuesAsync();
        return Ok(result);
    }
}
