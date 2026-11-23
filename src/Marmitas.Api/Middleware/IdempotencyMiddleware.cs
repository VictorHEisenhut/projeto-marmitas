using System.Security.Claims;
using System.Text;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;

namespace Marmitas.Api.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IPaymentRepository payments)
    {
        if (!HttpMethods.IsPost(context.Request.Method) && !HttpMethods.IsPatch(context.Request.Method))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var normalizedPath = (context.Request.Path.Value ?? string.Empty)
            .Replace("/api/v1/", "/api/", StringComparison.OrdinalIgnoreCase);

        var existing = await payments.GetIdempotencyRecordAsync(idempotencyKey, normalizedPath);
        if (existing != null)
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Idempotent-Replay"] = "true";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(responseBody))
            {
                var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
                Guid.TryParse(sub, out var userId);

                var doubleCheck = await payments.GetIdempotencyRecordAsync(idempotencyKey, normalizedPath);
                if (doubleCheck == null)
                {
                    await payments.AddIdempotencyRecordAsync(new IdempotencyRecord
                    {
                        Key = idempotencyKey,
                        RequestPath = normalizedPath,
                        UserId = userId == Guid.Empty ? null : userId,
                        StatusCode = context.Response.StatusCode,
                        ResponseBody = responseBody,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    });
                    await payments.SaveChangesAsync();
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
