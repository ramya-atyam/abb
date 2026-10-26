using System.Text.Json;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (GameRuleException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new ApiError(exception.StatusCode, exception.Message),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new ApiError(500, "An unexpected error occurred."),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
}