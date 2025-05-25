using FluentValidation;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.Validators.Tcc;
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
}