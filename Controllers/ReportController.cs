
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Services;

namespace ReportGen.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly ICsvProcessingService _csvProcessingService;
    private readonly ReportGenDbContext _context;

    public ReportController(ICsvProcessingService csvProcessingService, ReportGenDbContext context)
    {
        _csvProcessingService = csvProcessingService; 
        _context = context;
    }

    [HttpPost("upload_csv")]
    public async Task<IActionResult> UploadCsvFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Пожалуйста, загрузите непустой файл.");
        }

        string fileName = file.FileName;

        if (Path.GetExtension(fileName).ToLower() != ".csv")
        {
            return BadRequest("Разрешены только файлы с расширением .csv");
        }

        try
        {   
            await _csvProcessingService.ProcessCsvFileAsync(fileName, file.OpenReadStream());
            // Здесь будет ваш основной код для обработки CSV:
            // - Парсинг файла (используя StreamReader file.OpenReadStream())
            // - Валидация данных
            // - Вставка в таблицу Value
            // - Расчет и обновление таблицы Result
            // ... (вызываете ваш сервис по обработке CSV)
            // await _csvProcessingService.ProcessCsvFile(originalFileName, file.OpenReadStream());

            return Ok($"Файл '{fileName}' успешно загружен и обработан.");
        }
        catch (InvalidOperationException ex) 
        {
            return BadRequest(ex.Message); 
        }
        catch (Exception ex) 
        {
            return StatusCode(500, "Произошла внутренняя ошибка сервера.");
        }
    }


}
