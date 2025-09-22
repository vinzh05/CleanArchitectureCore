using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class CorrelationIdMiddleware : IMiddleware
    {
        private const string CorrelationHeader = "X-Correlation-Id";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[CorrelationHeader] = correlationId;
            }

            context.Response.Headers[CorrelationHeader] = correlationId;

            await next(context);
        }
    }
}
