using System.Text.Json;

namespace Marmitas.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no servidor");
            var errorBody = new { errors = new[] { new { code = "ERROR", message = ex.Message } } };
            await WriteAsync(context, StatusCodes.Status400BadRequest, errorBody);
        }
    }

    private async Task WriteAsync(HttpContext context, int statusCode, object body)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
