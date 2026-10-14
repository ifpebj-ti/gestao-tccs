using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace gestaotcc.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/User/me/password")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MyPasswordController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2048)]
    [EnableRateLimiting("own-password")]
    public async Task<IActionResult> Change([FromBody] ChangeOwnPasswordDTO data,
        [FromServices] ChangeOwnPasswordUseCase useCase)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var result = await useCase.Execute(userId, data);
        return result.IsSuccess ? Ok(new { message = "Senha alterada. Entre novamente com a nova senha." })
            : StatusCode(result.ErrorDetails?.Status ?? 500, result.ErrorDetails);
    }
}
