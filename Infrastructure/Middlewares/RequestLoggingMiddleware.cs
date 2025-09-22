using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class RequestLoggingMiddleware : IMiddleware
    {
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation("Request {RequestId} {Method} {Path}", requestId, context.Request.Method, context.Request.Path);

            await next(context);

            _logger.LogInformation("Response {RequestId} {StatusCode}", requestId, context.Response.StatusCode);
        }
    }
}
