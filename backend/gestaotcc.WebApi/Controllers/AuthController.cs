using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.Auth;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

public class AuthController(ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Realiza login no sistema
    /// </summary>
    /// <returns>Token de acesso</returns>
    /// <response code="200">Usuário retornado com Sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não encontrado</response>
    /// <response code="409">Erro de conflito</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponseModel>> Login([FromBody] LoginDTO data, [FromServices] LoginUseCase loginUseCase)
    {
        var validator = new LoginValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToString());
        var useCaseResult = await loginUseCase.Execute(data.Email, data.Password);

        if (!useCaseResult.IsSuccess)
        {
            logger.LogError($"Erro ao realizar login");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        logger.LogInformation($"Login realizado com sucesso");
        return Ok(new LoginResponseModel(useCaseResult.Data.AccessToken));
    }

    /// <summary>
    /// Alterar senha do usuário
    /// </summary>
    /// <returns>Mensagem de sucesso na operação</returns>
    /// <response code="200">Senha alterada com sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não encontrado</response>
    /// <response code="409">Erro de conflito</response>
    [AllowAnonymous]
    [HttpPost("update-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageSuccessResponseModel>> Update([FromBody] UpdatePasswordDTO data, [FromServices] UpdatePasswordUseCase updatePasswordUseCase)
    {
        var validator = new UpdatePasswordValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToString());
        var useCaseResult = await updatePasswordUseCase.Execute(data);

        if (useCaseResult.IsFailure)
        {
            logger.LogError($"Erro ao alterar a senha");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        logger.LogInformation($"Senha alterada com sucesso");
        return Ok(new MessageSuccessResponseModel("Senha alterada com sucesso"));
    }

    /// <summary>
    /// Criar nova senha do primeiro acesso de usuário Aluno
    /// </summary>
    /// <returns>Mensagem de sucesso na operação</returns>
    /// <response code="200">Senha criada com sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não encontrado</response>
    /// <response code="409">Erro de conflito</response>
    [AllowAnonymous]
    [HttpPost("new-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageSuccessResponseModel>> NewPassword([FromBody] NewPasswordDTO data, [FromServices] NewPasswordUseCase newPasswordUseCase)
    {
        var validator = new NewPasswordValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToString());
        var useCaseResult = await newPasswordUseCase.Execute(data);

        if (useCaseResult.IsFailure)
        {
            logger.LogError($"Erro ao criar a nova senha");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        logger.LogInformation($"Senha criada com sucesso");
        return Ok(new MessageSuccessResponseModel("Senha criada com sucesso"));
    }
}
