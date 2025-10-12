using gestaotcc.Application.UseCases.Campi;
using gestaotcc.Domain.Dtos.Campi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CampiController: ControllerBase
{
    /// <summary>
    /// Retornar os campis com seus cursos
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpGet("all")]
    public async Task<ActionResult<List<FindAllCampiDTO>>> FindAllCampi([FromServices] FindAllCampiUseCase findAllCampiUseCase)
    {
        var result = await findAllCampiUseCase.Execute(); 
        return Ok(result.Data);
    }

    /// <summary>
    /// Buscar todos os cursos para aquele campus
    /// </summary>
    /// <remarks>
    /// O CampiCourseId virá do token
    /// </remarks>
    [Authorize]
    [HttpGet("all/courses")]
    public async Task<ActionResult<List<FindAllCourseByCampiCourseIdDTO>>> FindAllCourseByCourseCampiId(
        [FromServices] FindAllCourseByCampiCourseIdUseCase findAllCourseByCampiCourseIdUseCase)
    {
        var campiCourseId = User.FindFirst("campiCourseId")?.Value;
        if (campiCourseId == null) return Unauthorized();

        var result = await findAllCourseByCampiCourseIdUseCase.Execute(long.Parse(campiCourseId));

        return Ok(result.Data);
    }
}