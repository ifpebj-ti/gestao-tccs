using gestaotcc.Application.Gateways;
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
        long parsedCampiCourseId = 0;
        if (!string.IsNullOrEmpty(campiCourseId))
        {
            long.TryParse(campiCourseId, out parsedCampiCourseId);
        }

        var result = await findAllCourseByCampiCourseIdUseCase.Execute(parsedCampiCourseId);

        return Ok(result.Data);
    }

    /// <summary>
    /// Buscar todos os cursos disponíveis publicamente para cadastro
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public/courses")]
    public async Task<ActionResult<List<FindAllCourseByCampiCourseIdDTO>>> FindAllPublicCourses(
        [FromServices] ICourseGateway courseGateway)
    {
        var campis = await courseGateway.FindAllCampis();
        var courses = campis
            .SelectMany(c => c.CampiCourses.Select(cc => new FindAllCourseByCampiCourseIdDTO(cc.Course.Id, cc.Course.Name)))
            .DistinctBy(c => c.Id)
            .ToList();

        return Ok(courses);
    }

    /// <summary>
    /// Retornar os campis com seus cursos de forma pública
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public/all")]
    public async Task<ActionResult<List<FindAllCampiDTO>>> FindAllCampiPublic([FromServices] FindAllCampiUseCase findAllCampiUseCase)
    {
        var result = await findAllCampiUseCase.Execute(); 
        return Ok(result.Data);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult> CreateCampi(
        [FromServices] CreateCampiUseCase createCampiUseCase,
        [FromBody] CreateCampiDTO dto)
    {
        var result = await createCampiUseCase.Execute(dto);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCampi(
        [FromServices] UpdateCampiUseCase updateCampiUseCase,
        [FromRoute] long id,
        [FromBody] CreateCampiDTO dto)
    {
        var result = await updateCampiUseCase.Execute(new UpdateCampiDTO(id, dto.Name, dto.City));
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCampi(
        [FromServices] DeleteCampiUseCase deleteCampiUseCase,
        [FromRoute] long id)
    {
        var result = await deleteCampiUseCase.Execute(id);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }
}