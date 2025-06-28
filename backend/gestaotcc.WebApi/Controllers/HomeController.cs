using gestaotcc.Application.UseCases.Home;
using gestaotcc.Domain.Dtos.Home;
using gestaotcc.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class HomeController : ControllerBase
{
    /// <summary>
    /// Retorna as informações da home
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<GetInfoHomeDTO>> GetInfoHome([FromServices] GetInfoHomeUseCase getInfoHomeUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var useCaseResult = await getInfoHomeUseCase.Execute(long.Parse(userIdClaim));
        if (useCaseResult.IsFailure)
        {
            Log.Error("Erro ao criar usuário");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        Log.Information("Dados retornados com sucesso");
        return Ok(useCaseResult.Data);
    }
}