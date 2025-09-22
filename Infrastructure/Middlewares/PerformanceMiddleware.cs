using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class PerformanceMiddleware : IMiddleware
    {
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(ILogger<PerformanceMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var sw = Stopwatch.StartNew();
            await next(context);
            sw.Stop();

            if (sw.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Slow request {Path} took {Elapsed} ms", context.Request.Path, sw.ElapsedMilliseconds);
            }
        }
    }
}
