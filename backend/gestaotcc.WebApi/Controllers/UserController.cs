using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.User;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(ILogger<UserController> logger, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Adiciona um novo usuário no sistema
    /// </summary>
    /// <remarks>
    /// Para o campo Profile, pode ser as seguintes opções: ADMIN, COORDINATOR, SUPERVISOR, ADVISOR, STUDENT, BANKING ou LIBRARY
    /// </remarks>
    /// <returns>Mensagem de sucesso na operação</returns>
    /// <response code="200">Usuário adicionado com Sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não existe</response>
    /// <response code="409">Erro de conflito</response>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageSuccessResponseModel>> Create([FromBody] CreateUserDTO data, [FromServices] CreateUserUseCase createUserUseCase)
    {
        var validator = new CreateUserValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ToString());
        }

        var result = await createUserUseCase.Execute(data, configuration.GetValue<string>("COMBINATION_STRING_FOR_ACCESSCODE")!);

        if (result.IsFailure)
        {
            logger.LogInformation("Erro ao criar usuário");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound();
        }

        logger.LogInformation("Usuário criado com sucesso");
        return Ok(new MessageSuccessResponseModel(result.Message));
    }

    /// <summary>
    /// Busca um usuário por meio de filtros
    /// </summary>
    /// <returns>Usuário filtrado</returns>
    /// <response code="200">Usuário filtrado com Sucesso</response>
    /// <response code="401">Acesso não autorizado</response>
    /// <response code="404">Recurso não existe</response>
    /// <response code="409">Erro de conflito</response>
    [HttpGet("email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> FindUserByEmail([FromQuery] string email, [FromServices] FindUserByEmailUseCase findUserByEmailUseCase)
    {
        var useCaseResult = await findUserByEmailUseCase.Execute(email);
        if (useCaseResult.IsFailure)
        {
            logger.LogError("Erro ao filtrar usuário");

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        logger.LogInformation("Usuário filtrado com sucesso");
        return Ok(UserResponseMethods.CreateUserResponse(useCaseResult.Data));
    }

    /// <summary>
    /// Buscar usuários por um filtro
    /// </summary>
    [HttpGet("filter")]
    public async Task<ActionResult<List<FindAllUserByFilterDTO>>> FindAllByFilter([FromQuery] UserFilterDTO data,
        [FromServices] FindAllUserByFilterUseCase findAllUserByFilterUseCase)
    {
        var useCaseResult = await findAllUserByFilterUseCase.Execute(data);
        
        Log.Information("Usuários retonados com sucesso");

        return Ok(useCaseResult.Data);
    }
}
