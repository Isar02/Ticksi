using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Ticksi.Application.Common.Exceptions;

namespace API.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var traceId = ErrorResponse.TraceIdOf(httpContext);

        var (status, error) = exception switch
        {
            ValidationException e => (StatusCodes.Status400BadRequest,
                ErrorResponse.Validation(e.Errors.Select(f => (f.PropertyName, f.ErrorMessage)), traceId)),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, new ErrorResponse("unauthorized", e.Message, traceId)),
            ForbiddenException e => (StatusCodes.Status403Forbidden, new ErrorResponse("forbidden", e.Message, traceId)),
            NotFoundException e => (StatusCodes.Status404NotFound, new ErrorResponse("not_found", e.Message, traceId)),
            ConflictException e => (StatusCodes.Status409Conflict, new ErrorResponse("conflict", e.Message, traceId)),
            BadHttpRequestException e => (e.StatusCode,
                new ErrorResponse("bad_request", "The request could not be processed.", traceId)),
            _ => (StatusCodes.Status500InternalServerError,
                new ErrorResponse("server_error", "Something went wrong. Please try again.", traceId))
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request {Method} {Path} failed, trace {TraceId}",
                httpContext.Request.Method, httpContext.Request.Path, traceId);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
