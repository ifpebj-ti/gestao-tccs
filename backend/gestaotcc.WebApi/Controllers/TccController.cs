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
    /// O StatusTcc pode ser: COMPLETED e IN_PROGRESS.
    /// </remarks>
    [Authorize]
    [HttpGet("filter")]
    public async Task<ActionResult<List<FindAllTccByFilterDTO>>> FindAllTccByFilter([FromQuery] TccFilterDTO tccFilter,
        [FromServices] FindAllTccByFilterUseCase findAllTccByFilterUseCase)
    {
        Log.Information("Tccs Retornados com sucesso");

        var useCaseResult = await findAllTccByFilterUseCase.Execute(tccFilter);

        return Ok(useCaseResult.Data);
    }

    /// <summary>
    /// Busca as informações de um tcc
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<FindTccDTO>> FindTcc([FromQuery] long tccId,
        [FromServices] FindTccUseCase findTccUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var result = await findTccUseCase.Execute(tccId);
        if (result.IsFailure)
        {
            Log.Error("Erro ao buscar TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            return NotFound(result.ErrorDetails);
        }
        Log.Information("TCC encontrado com sucesso");
        return Ok(result.Data);
    }

    /// <summary>
    /// Busca workflow de assinaturas de um tcc
    /// </summary>
    /// <remarks>Caso deseje retornar o workflow do tcc do usuário não mande o tccId</remarks>
    /// <param name="tccId"></param>
    [Authorize]
    [HttpGet("workflow")]
    public async Task<ActionResult<FindTccWorkflowDTO>> FindTccWorkflow([FromServices] FindTccWorkflowUseCase findWorkflowUseCase,
        [FromQuery] long? tccId)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var usecaseResult = await findWorkflowUseCase.Execute(tccId, long.Parse(userIdClaim));
        Log.Information("Workflow do tcc retornado com sucesso");
        return Ok(usecaseResult.Data);
    }

    /// <summary>
    /// Vincular usuário de banca para o TCC
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPost("banking")]
    public async Task<ActionResult<MessageSuccessResponseModel>> LinkBankingUser([FromBody] LinkBankingUserDTO data,
        [FromServices] LinkBankingUserUseCase linkBankingUserUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var result = await linkBankingUserUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro ao vincular usuário de banca");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Usuário de banca vinculado com sucesso");
        return Ok(new MessageSuccessResponseModel("Usuário de banca vinculado com sucesso"));
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
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPost("cancellation/approve")]
    public async Task<ActionResult<MessageSuccessResponseModel>> ApproveCancellation([FromQuery] long tccId,
        [FromServices] ApproveCancellationTccUseCase approveCancellationTccUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

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

    /// <summary>
    /// Visualizar solicitação de cancelamento do TCC
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpGet("cancellation")]
    public async Task<ActionResult<FindTccCancellationDTO>> FindTccCancellation([FromQuery] long tccId,
        [FromServices] FindTccCancellationUseCase findTccCancellationUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();

        var result = await findTccCancellationUseCase.Execute(tccId);
        if (result.IsFailure)
        {
            Log.Error("Erro ao buscar solicitação de cancelamento do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return NotFound(result.ErrorDetails);
        }

        Log.Information("Solicitação de cancelamento do TCC encontrada com sucesso");
        return Ok(result.Data);
    }

    /// <summary>
    /// Criar o agendamento de defesa do TCC
    /// </summary>
    /// <remarks>A localização do agendamento pode ser tanto uma sala física quanto um link de reunião online.</remarks>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPost("schedule")]
    public async Task<ActionResult<MessageSuccessResponseModel>> CreateSchedule([FromBody] ScheduleTccDTO data,
        [FromServices] CreateScheduleTccUseCase createScheduleTccUseCase)
    {
        var validator = new CreateScheduleTccValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var result = await createScheduleTccUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro ao criar agendamento de defesa do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Agendamento de defesa do TCC criado com sucesso");
        return Ok(new MessageSuccessResponseModel("Agendamento de defesa do TCC criado com sucesso"));
    }

    /// <summary>
    /// Editar o agendamento de defesa do TCC
    /// </summary>
    /// <remarks>A localização do agendamento pode ser tanto uma sala física quanto um link de reunião online.</remarks>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPut("schedule")]
    public async Task<ActionResult<MessageSuccessResponseModel>> EditSchedule([FromBody] ScheduleTccDTO data,
        [FromServices] EditScheduleTccUseCase editScheduleTccUseCase)
    {
        var validator = new EditScheduleTccValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var result = await editScheduleTccUseCase.Execute(data);
        if (result.IsFailure)
        {
            Log.Error("Erro ao editar agendamento de defesa do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Agendamento de defesa do TCC editado com sucesso");
        return Ok(new MessageSuccessResponseModel("Agendamento de defesa do TCC editado com sucesso"));
    }

    /// <summary>
    /// Enviar email com os dados do agendamento de defesa do TCC para os usuários vinculados
    /// </summary>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR, ADVISOR")]
    [HttpPost("schedule/email")]
    public async Task<ActionResult<MessageSuccessResponseModel>> SendScheduleEmail([FromQuery] long tccId,
        [FromServices] SendScheduleEmailUseCase sendScheduleEmailUseCase)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var result = await sendScheduleEmailUseCase.Execute(tccId);
        if (result.IsFailure)
        {
            Log.Error("Erro ao enviar email de agendamento de defesa do TCC");
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;
            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound(result.ErrorDetails);
        }
        Log.Information("Email de agendamento de defesa do TCC enviado com sucesso");
        return Ok(new MessageSuccessResponseModel("Email de agendamento de defesa do TCC enviado com sucesso"));
    }
}
