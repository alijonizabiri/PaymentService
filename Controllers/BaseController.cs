using Microsoft.AspNetCore.Mvc;
using PaymentService.Enums;
using PaymentService.Interfaces;
using PaymentService.Responses;

namespace PaymentService.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult MapError<T>(Result<T> result) => result.ErrorType switch
    {
        ErrorType.NotFound     => NotFound(new { error = result.Error }),
        ErrorType.Conflict     => Conflict(new { error = result.Error }),
        ErrorType.Forbidden    => Forbid(),
        ErrorType.Unauthorized => Unauthorized(new { error = result.Error }),
        ErrorType.Validation   => BadRequest(new { error = result.Error }),
        _                      => StatusCode(500, new { error = result.Error })
    };
}