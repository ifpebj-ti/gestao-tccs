using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.WebApi.InputModel;
using gestaotcc.WebApi.ResponseModels;
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

    /// <summary>
    /// Assinar um documento
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MessageSuccessResponseModel>> SignSignature([FromForm] SignSignatureInputModel data,
        [FromServices] SignSignatureUseCase signSignatureUseCase)
    {
        using Stream fileStream = data.File.OpenReadStream();
        var fileBuffer = new byte[fileStream.Length];

        using (fileStream)
        {
            await fileStream.ReadAsync(fileBuffer, 0, (int)fileStream.Length);
        }
        
        var dto = new SignSignatureDTO(
            data.TccId, 
            data.DocumentId, 
            data.UserId, 
            fileBuffer,
            (double)fileStream.Length / (1024 * 1024),
            data.File.ContentType
            );
        
        var useCaseResult = await signSignatureUseCase.Execute(dto);
        if (useCaseResult.IsFailure)
        {
            Log.Error(useCaseResult.ErrorDetails!.Detail);
            
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }
        
        Log.Information("Operação realizada com sucesso");
        return Ok(new MessageSuccessResponseModel("Operação realizada com sucesso"));
    }
}