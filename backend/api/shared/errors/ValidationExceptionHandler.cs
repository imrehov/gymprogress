using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace GymProgress.Api;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {

        //unexpected exception
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        //this path means the exception will be handled
        var errors = validationException.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        await Results.ValidationProblem(errors)
            .ExecuteAsync(httpContext);

        return true;
    }
}