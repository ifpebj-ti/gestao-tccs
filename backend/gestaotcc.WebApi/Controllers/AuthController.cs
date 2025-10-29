using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace gestaotcc.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(ILogger<AuthController> logger, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Buscar url para requisitar a autorização para login no gov.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("gov/login/url")]
    public async Task<ActionResult<ResponseGetAuthrizationDTO>> GenerateGovAuthLoginUrl([FromServices] GenerateGovLoginAuthUrlUseCase generateGovLoginAuthUrlUseCase)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var clientId = govSettings.GetValue<string>("CLIENT_ID_LOGIN");
        var scope = govSettings.GetValue<string>("SCOPE_LOGIN");
        var govUrlAuth = govSettings.GetValue<string>("URL_GOV_AUTH_LOGIN");
        var redirectUri = govSettings.GetValue<string>("REDIRECT_URL_GOV_LOGIN");

        var useCaseResult = await generateGovLoginAuthUrlUseCase.Execute(new GetAuthorizationLoginGovDTO(clientId, scope, redirectUri, govUrlAuth));

        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(new ResponseGetAuthrizationDTO(useCaseResult.Data));
    }
    
    /// <summary>
    /// Buscar url para requisitar a autorização para assinatura no gov.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("gov/signature/url")]
    public async Task<ActionResult<ResponseGetAuthrizationDTO>> GenerateGovAuthSignUrl([FromServices] GenerateGovSignatureAuthUrlUseCase generateGovSignatureAuthUrlUseCase)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var clientId = govSettings.GetValue<string>("CLIENT_ID_SING");
        var scope = govSettings.GetValue<string>("SCOPE_SIGN");
        var govUrlAuth = govSettings.GetValue<string>("URL_GOV_AUTH_SIGN");
        var redirectUri = govSettings.GetValue<string>("REDIRECT_URL_GOV_SIGN");

        var useCaseResult = await generateGovSignatureAuthUrlUseCase.Execute(new GetAuthorizationSignatureGovDTO(clientId, scope, redirectUri, govUrlAuth));

        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(new ResponseGetAuthrizationDTO(useCaseResult.Data));
    }

    /// <summary>
    /// Realizar login
    /// </summary>
    /// <param name="code">Código que o gov vai utilizar para validar.</param>
    /// <param name="state">Estado que o gov vai utilizar para validar.</param>
    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<ActionResult<TokenDTO>> Login(
        [FromServices] GetGovAccessTokenLoginUseCase getGovAccessTokenLoginUseCase,
        [FromQuery] string code,
        [FromQuery] string state)
    {
        var useCaseResult = await getGovAccessTokenLoginUseCase.Execute(code, state);
        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(useCaseResult.Data);
    }
    
    /// <summary>
    /// Busca accessToken para processo de assinatura eletrônica
    /// </summary>
    /// <param name="code">Código que o gov vai utilizar para validar.</param>
    /// <param name="state">Estado que o gov vai utilizar para validar.</param>
    [AllowAnonymous]
    [HttpGet("accesstoken/signature")]
    public async Task<ActionResult<TokenDTO>> GetSignAccessToken(
        [FromServices] GetGovAccessTokenSignatureUseCase getGovAccessTokenSignatureUseCase,
        [FromQuery] string code,
        [FromQuery] string state)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var redirectUri = govSettings.GetValue<string>("REDIRECT_URL_GOV_SIGN");
        
        var useCaseResult = await getGovAccessTokenSignatureUseCase.Execute(code, redirectUri);
        if (useCaseResult.IsFailure)
        {
            // Construindo a URL dinamicamente
            var endpointUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            useCaseResult.ErrorDetails!.Type = endpointUrl;

            // Retornando erro apropriado
            return useCaseResult.ErrorDetails?.Status is 500
                ? StatusCode(StatusCodes.Status500InternalServerError, useCaseResult.ErrorDetails)
                : NotFound(useCaseResult.ErrorDetails);
        }

        return Ok(useCaseResult.Data);
    }
}
