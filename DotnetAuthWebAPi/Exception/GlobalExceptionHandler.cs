using DotnetAuthWebAPi.Domain.Contract;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace DotnetAuthWebAPi.Exception
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, 
            System.Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, exception.Message);
            var response = new ErrorResponse()
            { Message=exception.Message};

            switch(exception)
            {
                case BadHttpRequestException:
                    response.StatusCode=(int)HttpStatusCode.BadRequest; 
                    response.Title =  exception.GetType().Name;
                    break;
                default:
                    response.StatusCode= (int)HttpStatusCode.BadRequest;    
                    response.Title= exception.GetType().Name;
                    break;

            }
            httpContext.Response.StatusCode = response.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(response,cancellationToken);

            return true;
        }
    }
}
