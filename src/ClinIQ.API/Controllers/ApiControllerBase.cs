using ClinIQ.Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClinIQ.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Problem(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return Problem(statusCode: StatusCodes.Status500InternalServerError);

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
                modelState.AddModelError(error.Field ?? error.Code, error.Description);

            return ValidationProblem(modelState);
        }

        var firstError = errors[0];
        var statusCode = firstError.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            title: statusCode == 500 ? "Server.Error" : firstError.Code,
            detail: statusCode == 500 ? "An unexpected error occurred." : firstError.Description);
    }
}
