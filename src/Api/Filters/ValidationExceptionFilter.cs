using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException validationException)
        {
            return;
        }

        context.Result = new BadRequestObjectResult(
            validationException.Errors.Select(error => error.ErrorMessage));
        context.ExceptionHandled = true;
    }
}
