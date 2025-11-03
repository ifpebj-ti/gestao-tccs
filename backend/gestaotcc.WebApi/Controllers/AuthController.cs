using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.Auth;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;


namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
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
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(new LoginResponseModel(useCaseResult.Data.AccessToken, useCaseResult.Data.IsDevTest, useCaseResult.Data.IsTempDevTestPassword));
    }

    /// <summary>
    /// Alterar senha do usuário
    /// </summary>
    [AllowAnonymous]
    [HttpPost("update-password")]
    public async Task<ActionResult<MessageSuccessResponseModel>> Update([FromBody] UpdatePasswordDTO data, [FromServices] UpdatePasswordUseCase updatePasswordUseCase)
    {
        var validator = new UpdatePasswordValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidatorException(validationResult.ToString());
        var useCaseResult = await updatePasswordUseCase.Execute(data);

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
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(new MessageSuccessResponseModel("Senha criada com sucesso"));
    }
}
