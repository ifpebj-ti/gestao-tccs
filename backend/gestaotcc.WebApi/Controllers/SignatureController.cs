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
public class SignatureController(IConfiguration configuration) : ControllerBase
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
        var campiIdClaim = User.FindFirst("campiCourseId")?.Value;
        if (campiIdClaim == null) return Unauthorized();
        
        var useCaseResult = await findAllPendingSignaturesUseCase.Execute(userId, long.Parse(campiIdClaim));
        
        return Ok(useCaseResult.Data);
    }

    /// <summary>
    /// Assinar um documento
    /// </summary>
    [Authorize]
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
            data.File.ContentType,
            data.AccessToken
            );
        
        var useCaseResult = await signSignatureUseCase.Execute(dto);
        if (useCaseResult.IsFailure)
        {
            
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }
        
        return Ok(new MessageSuccessResponseModel("Operação realizada com sucesso"));
    }

    /// <summary>
    /// Faz o download do documento
    /// </summary>
    /// <param name="tccId"> Id do tcc</param>
    /// <param name="documentId">Id do documento</param>
    [Authorize]
    [HttpGet("document/download")]
    public async Task<ActionResult> DownloaDocument([FromQuery] long tccId, [FromQuery] long documentId,
        [FromServices] DownloadDocumentUseCase downloadDocumentUseCase)
    {
        var useCaseResult = await downloadDocumentUseCase.Execute(tccId, documentId);
        if (useCaseResult.IsFailure)
        {
            
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }
        
        return File(useCaseResult.Data.File, "application/octet-stream", useCaseResult.Data.FileName);
    }

    /// <summary>
    /// Buscar um documento
    /// </summary>
    /// <remarks>
    /// Obs: A Url tem um tempo de experição de 60 segundos
    /// </remarks>
    /// <param name="tccId">Id do tcc</param>
    /// <param name="documentId">Id do documento</param>
    [Authorize]
    [HttpGet("document")]
    public async Task<ActionResult<FindDocumentDTO>> FindDocument(
        [FromQuery] long tccId, 
        [FromQuery] long documentId, 
        [FromServices] FindDocumentUseCase findDocumentUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();
        var campiCourseId = User.FindFirst("campiCourseId")?.Value;
        if (campiCourseId == null) return Unauthorized();
        
        var useCaseResult = await findDocumentUseCase.Execute(tccId, documentId, long.Parse(userIdClaim), long.Parse(campiCourseId));
        if (useCaseResult.IsFailure)
        {
            
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }
        
        return Ok(useCaseResult.Data);
    }

    /// <summary>
    /// Faz download de todos os documentos do tcc
    /// </summary>
    /// <param name="tccId">Id do tcc</param>
    [Authorize]
    [HttpGet("all/documents/download/{tccId}")]
    public async Task<ActionResult> AllDownloadDocuments([FromRoute] long tccId,
        [FromServices] AllDownloadDocumentsUseCase allDownloadDocumentsUseCase)
    {
        var useCaseResult = await allDownloadDocumentsUseCase.Execute(tccId);
        if (useCaseResult.IsFailure)
        {
            
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }
        
        return File(useCaseResult.Data.File, "application/zip", $"{useCaseResult.Data.FolderName}.zip");
    }
}