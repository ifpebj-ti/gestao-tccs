using gestaotcc.Application.UseCases.Course;
using gestaotcc.Domain.Dtos.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CourseController : ControllerBase
{
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult> CreateCourse(
        [FromServices] CreateCourseUseCase createCourseUseCase,
        [FromBody] CreateCourseDTO dto)
    {
        var result = await createCourseUseCase.Execute(dto);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCourse(
        [FromServices] UpdateCourseUseCase updateCourseUseCase,
        [FromRoute] long id,
        [FromBody] CreateCourseDTO dto)
    {
        var result = await updateCourseUseCase.Execute(new UpdateCourseDTO(id, dto.Name, dto.Level));
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCourse(
        [FromServices] DeleteCourseUseCase deleteCourseUseCase,
        [FromRoute] long id)
    {
        var result = await deleteCourseUseCase.Execute(id);
        if (!result.IsSuccess) return BadRequest(result.Message);
        return Ok();
    }
}
