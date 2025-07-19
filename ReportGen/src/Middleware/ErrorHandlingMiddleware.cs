using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ReportGen.Models;


namespace ReportGen.Middleware;
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "Произошла внутренняя ошибка сервера.";

        if (exception is InvalidOperationException || exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;

        }
        else if (exception is CsvValidationException csvEx)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(csvEx.Message);
            if (csvEx.Errors.Any())
            {
            sb.AppendLine("Детали ошибок валидации:");

                foreach (var error in csvEx.Errors)
                {
                    sb.AppendLine($"- Номер строки: {error.RowNumber}, Название колонки: {error.ColumnName}, Ошибка: {error.Message}, Значение: {error.Value}");
                }
            }
            
            statusCode = HttpStatusCode.BadRequest;
            message = sb.ToString();
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = statusCode,
            message = message,
        };

        var jsonOptions = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}