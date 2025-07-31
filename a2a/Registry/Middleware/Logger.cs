using System.Diagnostics;

namespace Registry.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var request = context.Request;

            await _next(context);

            sw.Stop();

            var response = context.Response;

            var method = request.Method;
            var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            var statusCode = response.StatusCode;
            var contentType = response.ContentType ?? "-";
            var elapsedMs = sw.Elapsed.TotalMilliseconds.ToString("0.####") + "ms";

            _logger.LogInformation("HTTP/{HttpVersion} {Method} {Url} - {StatusCode} - {ContentType} {Elapsed}",
                context.Request.Protocol.Replace("HTTP/", ""),
                method,
                url,
                statusCode,
                contentType,
                elapsedMs
            );
        }
    }
}