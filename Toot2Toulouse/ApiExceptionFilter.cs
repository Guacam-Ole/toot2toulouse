using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Toot2ToulouseWeb
{
    public class ApiExceptionFilter : IActionFilter, IOrderedFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context)
        { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                if (context.Exception is ApiException apiException)
                {
                    context.Result = new JsonResult(new { Error = apiException.ErrorType, Success = false, ErrorMessage = apiException.Message, apiException.StatusCode });
                    _logger.LogError(context.Exception, "ApiException");
                    context.ExceptionHandled = true;
                }
                else 
                {
                    context.Result = new JsonResult(new { Error = ApiException.ErrorTypes.Exception, Success = false, ErrorMessage = context.Exception.Message });
                    _logger.LogError(context.Exception, "general exception");
                    context.ExceptionHandled = true;
                }
            }
        }
    }
}