using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.Auth;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.RateLimiting;


namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequestSizeLimit(16 * 1024)]
[EnableRateLimiting("public-auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Realiza login no sistema
    /// </summary>
    /// <remarks>
    /// A propriedade isDevTest implica dizer que é um usuario do tipo teste ou não. Se for true é um usuario tipo teste.
    /// A proprieda IsTempDevTestPassword implica dizer que a senha ainda é a padrão ou não. Se for true ainda é.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseModel>> Login([FromBody] LoginDTO data, [FromServices] LoginUseCase loginUseCase)
    {
        var validator = new LoginValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        var useCaseResult = await loginUseCase.Execute(data.Email, data.Password);

        if (!useCaseResult.IsSuccess)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return StatusCode(useCaseResult.ErrorDetails?.Status ?? 400, useCaseResult.ErrorDetails);
        }

        return Ok(new LoginResponseModel(useCaseResult.Data.AccessToken, useCaseResult.Data.IsDevTest, useCaseResult.Data.IsTempDevTestPassword));
    }

    /// <summary>
    /// Alterar senha do usuário
    /// </summary>
    [AllowAnonymous]
    [HttpPost("update-password")]
    public async Task<ActionResult<MessageSuccessResponseModel>> Update([FromBody] UpdatePasswordDTO data, [FromServices] UpdatePasswordUseCase updatePasswordUseCase,
        [FromServices] PasswordResetSession resetSession)
    {
        var validator = new UpdatePasswordValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        if (!await resetSession.Validate(HttpContext, data.UserEmail))
            return Unauthorized(new { message = "Valide novamente o código de recuperação neste navegador." });
        var useCaseResult = await updatePasswordUseCase.Execute(data);

        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return StatusCode(useCaseResult.ErrorDetails?.Status ?? 400, useCaseResult.ErrorDetails);
        }

        resetSession.Clear(HttpContext);
        return Ok(new MessageSuccessResponseModel("Senha alterada com sucesso"));
    }

    /// <summary>
    /// Criar nova senha do primeiro acesso de usuário Aluno
    /// </summary>
    [AllowAnonymous]
    [HttpPost("new-password")]
    public async Task<ActionResult<MessageSuccessResponseModel>> NewPassword([FromBody] NewPasswordDTO data, [FromServices] NewPasswordUseCase newPasswordUseCase)
    {
        var validator = new NewPasswordValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        var useCaseResult = await newPasswordUseCase.Execute(data);

        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return StatusCode(useCaseResult.ErrorDetails?.Status ?? 400, useCaseResult.ErrorDetails);
        }

        return Ok(new MessageSuccessResponseModel("Senha criada com sucesso"));
    }

    /// <summary>
    /// Realiza login no sistema para avaliador externo
    /// </summary>
    [AllowAnonymous]
    [HttpPost("banking-login")]
    public async Task<ActionResult<LoginResponseModel>> BankingLogin([FromBody] gestaotcc.WebApi.InputModel.BankingLoginInputModel data, [FromServices] BankingLoginUseCase bankingLoginUseCase)
    {
        if (string.IsNullOrWhiteSpace(data.Token) || data.Token.Length > 4096)
            return BadRequest(new { message = "Token não informado." });

        var useCaseResult = await bankingLoginUseCase.Execute(data.Token);

        if (!useCaseResult.IsSuccess)
        {
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            return useCaseResult.ErrorDetails?.Status is 401
                ? Unauthorized(useCaseResult.ErrorDetails)
                : useCaseResult.ErrorDetails?.Status is 403
                ? Forbid()
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(new LoginResponseModel(useCaseResult.Data.AccessToken, useCaseResult.Data.IsDevTest, useCaseResult.Data.IsTempDevTestPassword));
    }
}
