using HMS.Entities.Exceptions;
using System.Net;
using System.Text.Json;

namespace HMS.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate                    _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, message) = ex switch
            {
                NotFoundException      => (HttpStatusCode.NotFound,            ex.Message),
                ForbiddenException     => (HttpStatusCode.Forbidden,           ex.Message),
                RecordLockedException  => (HttpStatusCode.Forbidden,           ex.Message),
                BusinessRuleException  => (HttpStatusCode.UnprocessableEntity, ex.Message),
                DuplicateException     => (HttpStatusCode.Conflict,            ex.Message),
                AuthException          => (HttpStatusCode.Unauthorized,        ex.Message),
                _                      => (HttpStatusCode.InternalServerError,
                                           "An unexpected error occurred. Please try again.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            var response = JsonSerializer.Serialize(new
            {
                isSuccess  = false,
                message,
                statusCode = (int)statusCode
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return context.Response.WriteAsync(response);
        }
    }

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionMiddleware(
            this IApplicationBuilder app) =>
            app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
