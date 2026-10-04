using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace API.Errors;

public sealed record ErrorResponse(
    string Code,
    string Message,
    string TraceId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static string TraceIdOf(HttpContext httpContext) => Activity.Current?.Id ?? httpContext.TraceIdentifier;

    public static ErrorResponse ForStatus(int statusCode, string traceId) => statusCode switch
    {
        StatusCodes.Status401Unauthorized => new("unauthorized", "Please sign in to continue.", traceId),
        StatusCodes.Status403Forbidden => new("forbidden", "You do not have permission to do this.", traceId),
        StatusCodes.Status404NotFound => new("not_found", "The requested resource was not found.", traceId),
        _ => new("request_failed", ReasonPhrases.GetReasonPhrase(statusCode), traceId)
    };

    public static ErrorResponse Validation(IEnumerable<(string Field, string Message)> failures, string traceId)
    {
        var errors = failures
            .GroupBy(f => JsonNamingPolicy.CamelCase.ConvertName(f.Field))
            .ToDictionary(g => g.Key, g => g.Select(f => f.Message).Distinct().ToArray());

        var message = string.Join(" ", errors.Values.SelectMany(m => m).Distinct());

        return new ErrorResponse("validation_failed", message, traceId, errors);
    }

    public static ErrorResponse Validation(ModelStateDictionary modelState, string traceId) =>
        Validation(
            modelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => (entry.Key,
                    string.IsNullOrEmpty(error.ErrorMessage) ? "The value is invalid." : error.ErrorMessage))),
            traceId);
}
