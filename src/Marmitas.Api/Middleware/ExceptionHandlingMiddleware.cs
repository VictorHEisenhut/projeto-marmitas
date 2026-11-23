using System.Text.Json;
using Marmitas.Domain.Errors;
using Marmitas.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

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
        catch (DomainException dex)
        {
            var statusCode = dex.Kind switch
            {
                ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var body = new
            {
                success = false,
                traceId = context.TraceIdentifier,
                errors = new[] { new { code = dex.Code, message = dex.Message } }
            };

            await WriteAsync(context, statusCode, body);
        }
        catch (DbUpdateConcurrencyException cex)
        {
            logger.LogWarning(cex, "Conflito de concorrência detectado.");
            var body = new
            {
                success = false,
                traceId = context.TraceIdentifier,
                errors = new[]
                {
                    new
                    {
                        code = "CONCURRENCY_CONFLICT",
                        message = "O registro foi alterado simultaneamente por outra operação. Atualize e tente novamente."
                    }
                }
            };
            await WriteAsync(context, StatusCodes.Status409Conflict, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no servidor");
            var errorBody = new
            {
                success = false,
                traceId = context.TraceIdentifier,
                errors = new[] { new { code = "ERROR", message = ex.Message } }
            };
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
