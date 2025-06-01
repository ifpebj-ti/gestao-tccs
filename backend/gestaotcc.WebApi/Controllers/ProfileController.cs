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
    /// <returns>Perfis buscados</returns>
    /// <response code="200">Perfis buscados com Sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não existe</response>
    /// <response code="409">Erro de conflito</response>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpGet]
    public async Task<ActionResult<List<FindAllProfilesDTO>>> FindAllProfiles([FromServices] FindAllProfilesUseCase findAllProfilesUseCase)
    {
        var result = await findAllProfilesUseCase.Execute();
        if (result.IsFailure)
        {
            logger.LogInformation("Erro ao buscar perfis");
            return NotFound();
        }
        logger.LogInformation("Perfis buscados com sucesso");
        return Ok(result.Data);
    }
}
