using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Utils;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.User;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Para o campo de Shift, pode ser as seguintes opções: 1 = MORNING, 2 = AFTERNOON, 3 = NIGHT, 4 = DAYTIME
    /// </remarks>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpPost]
    public async Task<ActionResult<MessageSuccessResponseModel>> Create([FromBody] CreateUserDTO data, [FromServices] CreateUserUseCase createUserUseCase)
    {
        var validator = new CreateUserValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidatorException(validationResult.ToString());
        }

        var result = await createUserUseCase.Execute(data, configuration.GetValue<string>("COMBINATION_STRING_FOR_ACCESSCODE")!);

        if (result.IsFailure)
        {

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 409
                ? Conflict(result.ErrorDetails)
                : NotFound();
        }

        return Ok(new MessageSuccessResponseModel(result.Message));
    }

    /// <summary>
    /// Busca um usuário por meio do email
    /// </summary>
    [HttpGet("email")]
    public async Task<ActionResult<FindUserByEmailDTO>> FindUserByEmail([FromQuery] string email, [FromServices] FindUserByEmailUseCase findUserByEmailUseCase)
    {
        var useCaseResult = await findUserByEmailUseCase.Execute(email);
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
    /// Buscar usuário por Id
    /// </summary>
    /// <param name="userId">Id do usuário.</param>
    [Authorize]
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserResponse>> FindById([FromRoute] long userId, [FromServices] FindUserByIdUseCase findUserByIdUseCase)
    {
        var useCaseResult = await findUserByIdUseCase.Execute(userId);
        if(useCaseResult.IsFailure)
        {

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 409
                ? Conflict(useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(UserResponseMethods.CreateUserResponseToFindUserById(useCaseResult.Data));
    }

    /// <summary>
    /// Buscar usuários por um filtro
    /// </summary>
    /// <remarks>
    /// Para o profile temos: ADMIN, COORDINATOR, SUPERVISOR, ADVISOR, STUDENT, BANKING, LIBRARY
    /// </remarks>
    [Authorize]
    [HttpGet("filter")]
    public async Task<ActionResult<List<FindAllUserByFilterDTO>>> FindAllByFilter([FromQuery] UserFilterDTO data,
        [FromServices] FindAllUserByFilterUseCase findAllUserByFilterUseCase)
    {
        var validator = new FindAllByFilterValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidatorException("teste");
        }
        
        var campiCourseId = User.FindFirst("campiCourseId")?.Value;
        if (campiCourseId == null) return Unauthorized();
        
        var useCaseResult = await findAllUserByFilterUseCase.Execute(data, long.Parse(campiCourseId));
        
        return Ok(useCaseResult.Data);
    }

    /// <summary>
    /// Auto cadastro de estudantes
    /// Para o campo de Shift, pode ser as seguintes opções: 1 = MORNING, 2 = AFTERNOON, 3 = NIGHT, 4 = DAYTIME
    /// </summary>
    [HttpPost("autoregister")]
    public async Task<ActionResult<MessageSuccessResponseModel>> AutoRegister([FromBody] AutoRegisterDTO data,
        [FromServices] AutoRegisterUseCase autoRegisterUseCase)
    {
        var validator = new AutoRegisterValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidatorException(validationResult.ToString());
        }

        var result = await autoRegisterUseCase.Execute(data, configuration.GetValue<string>("COMBINATION_STRING_FOR_ACCESSCODE")!);

        if (result.IsFailure)
        {

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, result.ErrorDetails)
                : NotFound();
        }

        return Ok(new MessageSuccessResponseModel(result.Message));
    }

    /// <summary>
    /// Atualizar informações do usuário
    /// </summary>
    /// <remarks>
    /// Para o campo Profile, pode ser as seguintes opções: ADMIN, COORDINATOR, SUPERVISOR, ADVISOR, STUDENT, BANKING ou LIBRARY
    /// Para o campo status, pode ser as seguintes opções: INACTIVE e ACTIVE
    /// Para o campo de Shift, pode ser as seguintes opções: 1 = MORNING, 2 = AFTERNOON, 3 = NIGHT, 4 = DAYTIME
    /// </remarks>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpPut]
    public async Task<ActionResult<MessageSuccessResponseModel>> Update([FromBody] UpdateUserDTO data, [FromServices] UpdateUserUseCase updateUserUseCase)
    {
        var validator = new UpdateUserValidator();
        var validationResult = await validator.ValidateAsync(data);
        if (!validationResult.IsValid)
        {
            throw new ValidatorException(validationResult.ToString());
        }

        var result = await updateUserUseCase.Execute(data);

        if (result.IsFailure)
        {

            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            result.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return result.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, result.ErrorDetails)
                : NotFound();
        }

        return Ok(new MessageSuccessResponseModel(result.Message));
    }

    /// <summary>
    /// Buscar todos os usuários com paginação.
    /// </summary>
    /// <param name="pageNumber">Número da página.</param>
    /// <param name="pageSize">Tamanho da página.</param>
    [Authorize(Roles = "ADMIN, COORDINATOR, SUPERVISOR")]
    [HttpGet("all")]
    public async Task<ActionResult<PagedResultResponse<List<FindAllUserDTO>>>> FindAll(
        [FromServices] FindAllUserUseCase findAllUserUseCase,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await findAllUserUseCase.Execute(pageNumber, pageSize);

        return Ok(result.Data);
    }
}
