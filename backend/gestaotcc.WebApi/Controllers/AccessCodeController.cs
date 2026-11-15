using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.AccessCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.RateLimiting;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequestSizeLimit(16 * 1024)]
[EnableRateLimiting("public-auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccessCodeController(ILogger<AccessCodeController> logger, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Verifica o código de acesso
    /// </summary>
    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<ActionResult<MessageSuccessResponseModel>> VerifyCode([FromBody] VerifyAccessCodeDTO data, [FromServices] VerifyAccessCodeUseCase verifyAccessCodeUseCase,
        [FromServices] PasswordResetSession resetSession)
    {
        var validator = new VerifyAccessCodeValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        var useCaseResult = await verifyAccessCodeUseCase.Execute(data);

        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return StatusCode(useCaseResult.ErrorDetails?.Status ?? 400, useCaseResult.ErrorDetails);
        }

        await resetSession.Issue(HttpContext, data.UserEmail);
        return Ok(new MessageSuccessResponseModel("Código de acesso verificado com sucesso"));
    }

    /// <summary>
    /// Renvia o código de acesso
    /// </summary>
    [AllowAnonymous]
    [HttpPost("resend")]
    public async Task<ActionResult<MessageSuccessResponseModel>> ResendAccessCode([FromBody] ResendAccessCodeDTO data, [FromServices] ResendAccessCodeUseCase resendAccessCodeUseCase)
    {
        var validator = new ResendAccessCodeValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        var useCaseResult = await resendAccessCodeUseCase.Execute(data, configuration.GetValue<string>("COMBINATION_STRING_FOR_ACCESSCODE")!);

        if (useCaseResult.IsFailure)
        {

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return StatusCode(useCaseResult.ErrorDetails?.Status ?? 400, useCaseResult.ErrorDetails);
        }

        return Ok(new MessageSuccessResponseModel("Código de acesso reenviado com sucesso"));
    }
}
