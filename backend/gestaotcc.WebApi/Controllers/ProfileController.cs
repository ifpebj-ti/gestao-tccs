using gestaotcc.Application.UseCases.Profile;
using gestaotcc.Domain.Dtos.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProfileController(ILogger<UserController> logger, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Busca todos os perfis de usuário disponíveis no sistema
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpGet]
    public async Task<ActionResult<List<FindAllProfilesDTO>>> FindAllProfiles([FromServices] FindAllProfilesUseCase findAllProfilesUseCase)
    {
        var result = await findAllProfilesUseCase.Execute();
        if (result.IsFailure)
        {
            return NotFound();
        }
        return Ok(result.Data);
    }
}
