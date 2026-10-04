namespace API.Errors;

public static class ErrorHandlingExtensions
{
    public static WebApplication UseApiErrorResponses(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseStatusCodePages(context =>
        {
            var httpContext = context.HttpContext;
            var error = ErrorResponse.ForStatus(httpContext.Response.StatusCode, ErrorResponse.TraceIdOf(httpContext));
            return httpContext.Response.WriteAsJsonAsync(error);
        });

        return app;
    }
}
