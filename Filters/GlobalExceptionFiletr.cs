using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Transport.WebApi.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
  private readonly ILogger<GlobalExceptionFilter> _logger;
  private readonly IWebHostEnvironment _environment;

  public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment environment)
  {
    _logger = logger;
    _environment = environment;
  }

  public void OnException(ExceptionContext context)
  {
    _logger.LogError(context.Exception, "Unhandled exception occurred. Request: {Method} {Path}",
      context.HttpContext.Request.Method,
      context.HttpContext.Request.Path);

    object response;

    if (_environment.IsDevelopment())
    {
      response = new
      {
        error = context.Exception.Message,
        stackTrace = context.Exception.StackTrace,
        requestId = context.HttpContext.TraceIdentifier
      };
    }
    else
    {
      response = new
      {
        error = "An internal server error occurred",
        requestId = context.HttpContext.TraceIdentifier
      };
    }

    context.Result = new ObjectResult(response)
    {
      StatusCode = 500
    };

    context.ExceptionHandled = true;
  }
}
