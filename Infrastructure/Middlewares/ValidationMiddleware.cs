using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using ValidationException = FluentValidation.ValidationException;

namespace Infrastructure.Middlewares
{
    public class ValidationMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                var response = new
                {
                    status = context.Response.StatusCode,
                    title = "Validation failed",
                    errors
                };

                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
            }
        }
    }
}
