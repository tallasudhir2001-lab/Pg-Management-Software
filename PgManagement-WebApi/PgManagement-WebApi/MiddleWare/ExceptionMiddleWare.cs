namespace PgManagement_WebApi.MiddleWare
{
    public class ExceptionMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleWare> _logger;

        public ExceptionMiddleWare(RequestDelegate _next, ILogger<ExceptionMiddleWare> logger)
        {
            this._next = _next;
            this._logger = logger; 
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var userId = context.User?.FindFirst("userId")?.Value ?? "anonymous";
                var method = context.Request.Method;
                var path = context.Request.Path;

                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path} by user {UserId}",
                    method, path, userId);

                await HandleExceptionAsync(context, ex);
            }
        }
        private static Task HandleExceptionAsync(
           HttpContext context,
           Exception exception)
        {
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = exception.Message
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
