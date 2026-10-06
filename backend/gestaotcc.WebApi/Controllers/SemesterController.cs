using gestaotcc.Application.UseCases.Semester;
using gestaotcc.Domain.Dtos.Semester;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SemesterController : ControllerBase
{
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpGet("all")]
    public async Task<ActionResult<List<SemesterDTO>>> FindAll([FromServices] FindAllSemestersUseCase findAllUseCase)
    {
        var result = await findAllUseCase.Execute();
        return Ok(result.Data);
    }

    [Authorize] // Qualquer usuário logado pode precisar saber os semestres ativos para filtrar
    [HttpGet("active")]
    public async Task<ActionResult<List<SemesterDTO>>> FindActive([FromServices] FindActiveSemestersUseCase findActiveUseCase)
    {
        var result = await findActiveUseCase.Execute();
        return Ok(result.Data);
    }

    [Authorize(Roles = "ADMIN, COORDINATOR")]
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromServices] CreateSemesterUseCase createUseCase,
        [FromBody] CreateSemesterDTO dto)
    {
        var result = await createUseCase.Execute(dto);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [Authorize(Roles = "ADMIN, COORDINATOR")]
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(
        [FromServices] UpdateSemesterUseCase updateUseCase,
        [FromRoute] long id,
        [FromBody] CreateSemesterDTO dto)
    {
        var result = await updateUseCase.Execute(id, dto);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(
        [FromServices] DeleteSemesterUseCase deleteUseCase,
        [FromRoute] long id)
    {
        var result = await deleteUseCase.Execute(id);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }
}
