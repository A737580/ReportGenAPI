using System.Net;
using System.Text.Json;
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

        if (exception is InvalidOperationException || exception is ArgumentException|| exception is CsvValidationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
            
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = statusCode,
            message = message,
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}