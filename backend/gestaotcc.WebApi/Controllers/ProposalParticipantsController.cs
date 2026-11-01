using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "STUDENT")]
[Route("api/Tcc/participant-invitations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProposalParticipantsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromServices] ProposalParticipantsUseCase useCase)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Response(await useCase.List(userId));
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Respond(long id, [FromBody] RespondToProposalDTO data,
        [FromServices] ProposalParticipantsUseCase useCase)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        if (!data.Accept.HasValue) return BadRequest(new { detail = "Informe se deseja aceitar ou recusar." });
        return Response(await useCase.Respond(id, userId, data.Accept.Value));
    }

    [HttpGet("proposal/{id:long}")]
    public async Task<IActionResult> Status(long id, [FromServices] ProposalParticipantsUseCase useCase)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Response(await useCase.Status(id, userId));
    }

    private bool TryUser(out long id) => long.TryParse(User.FindFirst("userId")?.Value, out id) && id > 0;
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromServices] ProposalParticipantsUseCase useCase)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Response(await useCase.Notifications(userId));
    }

    [HttpPatch("{id:long}/notification")]
    public async Task<IActionResult> Dismiss(long id, [FromServices] ProposalParticipantsUseCase useCase)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Response(await useCase.Dismiss(id, userId));
    }
    private IActionResult Response<T>(ResultPattern<T> result) => result.IsSuccess ? Ok(result.Data) : StatusCode(result.ErrorDetails?.Status ?? 500, result.ErrorDetails);
}

public record RespondToProposalDTO(bool? Accept);
