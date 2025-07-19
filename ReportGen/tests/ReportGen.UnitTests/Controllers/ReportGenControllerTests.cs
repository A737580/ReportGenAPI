using Moq;
using Microsoft.AspNetCore.Mvc;
using ReportGen.Controllers;
using ReportGen.Interfaces;
using ReportGen.Models; 
using ReportGen.Services; 
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace ReportGen.UnitTests.Controllers;

public class ReportControllerTests
{
    private readonly Mock<IResultRepository> _mockResultRepository;
    private readonly Mock<ICsvProcessingService> _mockCsvProcessingService;
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _mockResultRepository = new Mock<IResultRepository>();
        _mockCsvProcessingService = new Mock<ICsvProcessingService>();
        _controller = new ReportController(_mockResultRepository.Object, _mockCsvProcessingService.Object);
    }


    [Fact]
    public async Task UploadCsvFile_ShouldReturnBadRequest_WhenFileIsNull()
    {
        var result = await _controller.UploadCsvFile(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Загрузите непустой файл.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadCsvFile_ShouldReturnBadRequest_WhenFileIsEmpty()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0); 

        var result = await _controller.UploadCsvFile(mockFile.Object);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Загрузите непустой файл.", badRequestResult.Value);
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("image.jpg")]
    [InlineData("document.pdf")]
    public async Task UploadCsvFile_ShouldReturnBadRequest_WhenFileIsNotCsv(string fileName)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100); 
        mockFile.Setup(f => f.FileName).Returns(fileName);

        var result = await _controller.UploadCsvFile(mockFile.Object);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Разрешены только файлы с расширением .csv", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadCsvFile_ShouldReturnOk_WhenCsvFileIsProcessedSuccessfully()
    {
        var fileName = "test.csv";
        var fileContent = @"Date;ExecutionTime;Value\n2024-07-16T10-05-00.0000Z;15;120.0\n2024-07-16T10-06-00.0000Z;14;000.7";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        _mockCsvProcessingService.Setup(s => s.ProcessCsvFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>()))
            .Returns(Task.CompletedTask); 

        var result = await _controller.UploadCsvFile(mockFile.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal($"Файл '{fileName}' успешно загружен и обработан.", okResult.Value);

        _mockCsvProcessingService.Verify(s => s.ProcessCsvFileAsync(fileName, It.IsAny<Stream>()), Times.Once());
    }

    [Fact]
    public async Task UploadCsvFile_ShouldHandleExceptionFromCsvProcessingService()
    {
        var fileName = "invalid.csv";
        var fileContent = "header\ninvalid_data";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        _mockCsvProcessingService.Setup(s => s.ProcessCsvFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>()))
            .ThrowsAsync(new CsvValidationException("Ошибка валидации CSV", new List<CsvValidationError>())); 

        await Assert.ThrowsAsync<CsvValidationException>(() => _controller.UploadCsvFile(mockFile.Object));

    }

    [Fact]
    public async Task GetResultsByParameters_ShouldReturnBadRequest_WhenParametersAreNull()
    {
        var result = await _controller.GetResultsByParameters(null);

        var actionResult = Assert.IsType<ActionResult<IEnumerable<ResultResponceDto>>>(result);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);

        Assert.Equal("Параметры фильтрации не могут быть пустыми.", badRequestResult.Value);
    }

    [Theory]
    [InlineData("invalid-date-format", null, "Неверный формат даты. Пример формата даты: 2023-01-01T00-00-00.0000Z или 2024-07-16T16-30-00.1234Z.")]
    [InlineData(null, "another-invalid-date", "Неверный формат даты. Пример формата даты: 2023-01-01T00-00-00.0000Z или 2024-07-16T16-30-00.1234Z.")]
    public async Task GetResultsByParameters_ShouldReturnBadRequest_WhenDateFormatsAreInvalid(string minDate, string maxDate, string expectedErrorMessage)
    {
        var requestParameters = new ResultFilterParametersDto
        {
            MinMinimumDateTime = minDate,
            MaxMinimumDateTime = maxDate
        };
        var result = await _controller.GetResultsByParameters(requestParameters);

        var actionResult = Assert.IsType<ActionResult<IEnumerable<ResultResponceDto>>>(result);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);

        var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
        Assert.Contains(modelState.Keys, k => k == nameof(requestParameters.MinMinimumDateTime));
        Assert.Contains((IEnumerable<string>)modelState[nameof(requestParameters.MinMinimumDateTime)], m => m == expectedErrorMessage);
    }

    [Fact]
    public async Task GetResultsByParameters_ShouldReturnOkWithResults_WhenParametersAreValid()
    {
        var requestParameters = new ResultFilterParametersDto
        {
            FileName = "test.csv",
            MinMinimumDateTime = "2023-01-01T00-00-00.0000Z",
            MaxMinimumDateTime = "2023-12-31T23-59-59.0000Z",
            MinAvgExecutionTime = 10,
            MaxAvgExecutionTime = 20
        };

        string dateFormat = "yyyy-MM-ddTHH-mm-ss.ffffZ"; 
        var cultureInfo = CultureInfo.InvariantCulture;
        var dateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        var mockResults = new List<Result>
        {
            new Result {
                FileName = "test.csv",
                MinimumDateTime = DateTimeOffset.ParseExact("2023-06-01T10-00-00.0000Z", dateFormat, cultureInfo, dateTimeStyles),
                AvgExecutionTime = 15, AvgStoreValue = 100, MedianStoreValue = 100, MaximumStoreValue = 200, MinimumStoreValue = 50, DeltaTimeS = 1000
            },
            new Result {
                FileName = "test.csv",
                MinimumDateTime = DateTimeOffset.ParseExact("2023-07-15T12-30-00.0000Z", dateFormat, cultureInfo, dateTimeStyles),
                AvgExecutionTime = 18, AvgStoreValue = 120, MedianStoreValue = 120, MaximumStoreValue = 250, MinimumStoreValue = 60, DeltaTimeS = 1200
            }
        };

        _mockResultRepository.Setup(r => r.GetResultsByParametersAsync(
            It.IsAny<ResultFilterRepositoryDto>()))
            .ReturnsAsync(mockResults);

        var result = await _controller.GetResultsByParameters(requestParameters);

        var actionResult = Assert.IsType<ActionResult<IEnumerable<ResultResponceDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);  

        var resultDtos = Assert.IsAssignableFrom<IEnumerable<ResultResponceDto>>(okResult.Value);

        Assert.Equal(mockResults.Count, resultDtos.Count());
        Assert.Contains(resultDtos, dto => dto.FileName == "test.csv" && dto.AvgExecutionTime == 15);
        Assert.Contains(resultDtos, dto => dto.FileName == "test.csv" && dto.AvgExecutionTime == 18);

        _mockResultRepository.Verify(r => r.GetResultsByParametersAsync(
            It.Is<ResultFilterRepositoryDto>(dto =>
                dto.FileName == requestParameters.FileName &&
                dto.MinAvgExecutionTime == requestParameters.MinAvgExecutionTime &&  
                (dto.MinMinimumDateTime.HasValue ? dto.MinMinimumDateTime.Value.ToString(dateFormat, cultureInfo) : null) == requestParameters.MinMinimumDateTime &&
                (dto.MaxMinimumDateTime.HasValue ? dto.MaxMinimumDateTime.Value.ToString(dateFormat, cultureInfo) : null) == requestParameters.MaxMinimumDateTime
            )), Times.Once());
    }


    [Fact]
public async Task GetLatestValues_ShouldReturnNotFound_WhenNoValuesFound()
{
    var fileName = "non_existent.csv";
    _mockResultRepository.Setup(r => r.GetLatestValuesAsync(fileName))
        .ReturnsAsync(new List<Value>()); 

    var result = await _controller.GetLatestValues(fileName);

    var actionResult = Assert.IsType<ActionResult<IEnumerable<ValueResponceDto>>>(result);

    Assert.IsType<NotFoundResult>(actionResult.Result);
}

    [Fact]
public async Task GetLatestValues_ShouldReturnOkWithValues_WhenValuesAreFound()
{
    var fileName = "existing.csv";
    var mockValues = new List<Value>
    {
        new Value { Id = 1, FileName = fileName, StartDateTime = DateTimeOffset.Now, ExecutionTimeS = 10, StoreValue = 100 },
        new Value { Id = 2, FileName = fileName, StartDateTime = DateTimeOffset.Now.AddSeconds(1), ExecutionTimeS = 12, StoreValue = 120 }
    };

    _mockResultRepository.Setup(r => r.GetLatestValuesAsync(fileName))
        .ReturnsAsync(mockValues);

    var result = await _controller.GetLatestValues(fileName);

    var actionResult = Assert.IsType<ActionResult<IEnumerable<ValueResponceDto>>>(result);

    var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

    var resultDtos = Assert.IsAssignableFrom<IEnumerable<ValueResponceDto>>(okResult.Value);

    Assert.Equal(mockValues.Count, resultDtos.Count());
    Assert.Contains(resultDtos, dto => dto.FileName == fileName && dto.ExecutionTimeS == 10);
    Assert.Contains(resultDtos, dto => dto.FileName == fileName && dto.ExecutionTimeS == 12);

    _mockResultRepository.Verify(r => r.GetLatestValuesAsync(fileName), Times.Once());
}
}