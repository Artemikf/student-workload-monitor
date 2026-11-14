using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace student_workload_monitor.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title, detail) = exception switch
        {
            ArgumentException or InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Некорректные входные данные",
                exception.Message),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Ресурс не найден",
                exception.Message),

            JsonException => (
                StatusCodes.Status400BadRequest,
                "Ошибка формата JSON",
                "Передан невалидный JSON или несоответствующие типы полей."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка при обработке запроса.")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Критическая ошибка при обработке {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Клиентская ошибка ({StatusCode}) на {Path}: {Message}",
                statusCode, httpContext.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
