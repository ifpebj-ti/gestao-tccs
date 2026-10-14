using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/User/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MyProfileController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Find([FromServices] MyProfileUseCase useCase)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToResponse(await useCase.Find(userId));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMyProfileDTO data, [FromServices] MyProfileUseCase useCase)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToResponse(await useCase.UpdatePhone(userId, data));
    }

    // O cliente não escolhe a conta: o ID vem exclusivamente do JWT validado.
    [HttpPatch]
    [Authorize(Roles = "STUDENT")]
    public async Task<IActionResult> UpdateStudent([FromBody] UpdateStudentProfileDTO data, [FromServices] MyProfileUseCase useCase)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return ToResponse(await useCase.UpdateStudent(userId, data));
    }

    private bool TryGetUserId(out long userId) =>
        long.TryParse(User.FindFirst("userId")?.Value, out userId) && userId > 0;

    private IActionResult ToResponse(ResultPattern<MyProfileDTO> result) =>
        result.IsSuccess ? Ok(result.Data) : StatusCode(result.ErrorDetails?.Status ?? 500, result.ErrorDetails);
}
