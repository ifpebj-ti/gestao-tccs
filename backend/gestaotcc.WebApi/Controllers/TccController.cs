using FluentValidation;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.Validators.Tcc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TccController : ControllerBase
{
    /// <summary>
    /// Criar uma nova proposta de tcc
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpPost]
    public async Task<ActionResult<MessageSuccessResponseModel>> Create([FromBody] CreateTccDTO data,
        [FromServices] CreateTccUseCase createTccUseCase)
    {
        var validator = new CreateTccValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var result = await createTccUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro ao criar usuário");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound();
        }

        Log.Information("Usuário criado com sucesso");
        return Ok(new MessageSuccessResponseModel(result.Message));
    }
    /// <summary>
    /// Verificar código de primeiro acesso enviado no cadastro de proposta
    /// </summary>
    [AllowAnonymous]
    [HttpPost("code/verify")]
    public async Task<ActionResult<MessageSuccessResponseModel>> VerifyCode([FromBody] VerifyCodeInviteTccDTO data,
        [FromServices] VerifyCodeInviteTccUseCase verifyCodeInviteTccUseCase)
    {
        var validator = new VerifyCodeInviteValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var result = await verifyCodeInviteTccUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro verificar código");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }

        Log.Information("Código verificado com sucesso");
        return Ok(new MessageSuccessResponseModel("Código verificado com sucesso"));
    }

    /// <summary>
    /// Busca todos os tccs baseado no filtro
    /// </summary>
    /// <remarks>
    /// O filter pode ser: COMPLETED e IN_PROGRESS.
    /// Caso deseje retornar por id de usuário basta enviar o filter vazio ou null
    /// </remarks>
    /// <param name="filter"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("filter")]
    public async Task<ActionResult<List<FindAllTccByStatusOrUserIdDTO>>> FindAllTccByFilter([FromQuery] string filter,
        [FromServices] FindAllTccByFilterUseCase findAllTccByFilterUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        Log.Information("Tccs Retornados com sucesso");

        var useCaseResult = await findAllTccByFilterUseCase.Execute(filter, long.Parse(userIdClaim));

        return Ok(useCaseResult.Data);
    }

    /// <summary>
    /// Busca workflow de assinaturas de um tcc
    /// </summary>
    /// <remarks>Caso deseje retornar o workflow do tcc do usuário não mande o tccId</remarks>
    /// <param name="tccId"></param>
    [Authorize]
    [HttpGet("workflow")]
    public async Task<ActionResult<FindTccWorkflowDTO>> FindTccWorkflow([FromServices] FindTccWorkflowUseCase findWorkflowUseCase,
        [FromQuery] long tccId = 0)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var usecaseResult = await findWorkflowUseCase.Execute(tccId, long.Parse(userIdClaim));
        Log.Information("Workflow do tcc retornado com sucesso");
        return Ok(usecaseResult.Data);
    }

    /// <summary>
    /// Solicitar cancelamento do TCC
    /// </summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPost("cancellation/request")]
    public async Task<ActionResult<MessageSuccessResponseModel>> RequestCancellation([FromBody] RequestCancellationTccDTO data,
        [FromServices] RequestCancellationTccUseCase requestCancellationTccUseCase)
    {
        var validator = new RequestCancellationTccValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var result = await requestCancellationTccUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro ao solicitar cancelamento do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Solicitação de cancelamento do TCC realizada com sucesso");
        return Ok(new MessageSuccessResponseModel("Solicitação de cancelamento do TCC realizada com sucesso"));
    }

    /// <summary>
    /// Aprovar cancelamento do TCC
    /// </summary>
    //[Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPost("cancellation/approve")]
    public async Task<ActionResult<MessageSuccessResponseModel>> ApproveCancellation([FromQuery] long tccId,
        [FromServices] ApproveCancellationTccUseCase approveCancellationTccUseCase)
    {
        //var userIdClaim = User.FindFirst("userId")?.Value;
        //if (userIdClaim == null) return Unauthorized();

        var result = await approveCancellationTccUseCase.Execute(tccId);
        if (result.IsFailure)
        {
            Log.Error("Erro ao aprovar cancelamento do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Cancelamento do TCC aprovado com sucesso");
        return Ok(new MessageSuccessResponseModel("Cancelamento do TCC aprovado com sucesso"));
    }

}