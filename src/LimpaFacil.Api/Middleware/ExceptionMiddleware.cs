using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var (status, code) = ex switch
            {
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
                PriceChangedException => (StatusCodes.Status409Conflict, "price_changed"),
                InsufficientStockException => (StatusCodes.Status409Conflict, "insufficient_stock"),
                ConcurrencyException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict"),
                DomainException => (StatusCodes.Status422UnprocessableEntity, "business_rule"),
                _ => (StatusCodes.Status500InternalServerError, "internal_error")
            };
            if (status == 500) logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);
            else logger.LogInformation(ex, "Requisição recusada em {Path}", context.Request.Path);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ApiError(code,
                status == 500 ? "Ocorreu um erro interno. Use o identificador do log para suporte." : ex.Message));
        }
    }
}
