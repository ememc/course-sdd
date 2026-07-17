using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
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
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict,
                System.Collections.Generic.KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var problemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = exception switch
                {
                    FluentValidation.ValidationException => "Validación fallida",
                    Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => "Conflicto de concurrencia optimista",
                    System.Collections.Generic.KeyNotFoundException => "Recurso no encontrado",
                    _ => "Ha ocurrido un error al procesar la solicitud."
                },
                Detail = exception.Message
            };

            if (exception is FluentValidation.ValidationException valEx)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var err in valEx.Errors)
                {
                    if (!errors.ContainsKey(err.PropertyName))
                    {
                        errors[err.PropertyName] = new[] { err.ErrorMessage };
                    }
                    else
                    {
                        var list = new List<string>(errors[err.PropertyName]) { err.ErrorMessage };
                        errors[err.PropertyName] = list.ToArray();
                    }
                }
                
                var valProblemDetails = new ValidationProblemDetails(errors)
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Validación fallida",
                    Detail = "Uno o más campos del formulario contienen errores."
                };
                
                return context.Response.WriteAsync(JsonSerializer.Serialize(valProblemDetails));
            }

            var result = JsonSerializer.Serialize(problemDetails);
            return context.Response.WriteAsync(result);
        }
    }
}
