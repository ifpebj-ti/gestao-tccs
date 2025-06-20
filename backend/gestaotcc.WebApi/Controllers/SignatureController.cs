using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SignatureController : ControllerBase
{
    /// <summary>
    /// Busca todas as assinaturas pendentes
    /// </summary>
    /// <remarks>
    /// Caso deseje retornar apenas as assinaturas pendentes do usuário envio o id do mesmo
    /// </remarks>
    /// <param name="userId">Id do Usuário</param>
    [Authorize]
    [HttpGet("pending")]
    public async Task<ActionResult<List<FindAllPendingSignatureDTO>>> FindAllPendingSignatures([FromQuery] long? userId,
        [FromServices] FindAllPendingSignaturesUseCase findAllPendingSignaturesUseCase)
    {
        Log.Information("Retonando assinaturas pendentes com sucesso");
        var useCaseResult = await findAllPendingSignaturesUseCase.Execute(userId);
        
        return Ok(useCaseResult.Data);
    }
}