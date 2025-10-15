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
public class AuthController(ILogger<AuthController> logger, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Buscar url para requisitar a autorização no gov.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("gov/url")]
    public async Task<ActionResult<ResponseGetAuthrizationDTO>> GenerateGovAuthUrl([FromServices] GenerateGovAuthUrlUseCase generateGovAuthUrlUseCase)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var urlToken = govSettings.GetValue<string>("URL_TOKEN");
        var clientId = govSettings.GetValue<string>("CLIENT_ID");
        var scope = govSettings.GetValue<string>("SCOPE");
        var govUrlAuth = govSettings.GetValue<string>("URL_GOV_AUTH");
        var redirectUri = govSettings.GetValue<string>("REDIRECT_URL_GOV");

        var useCaseResult = await generateGovAuthUrlUseCase.Execute(new GetAuthorizationDTO(clientId, scope, redirectUri, govUrlAuth));

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
        [FromServices] GetGovAccessTokenUseCase getGovAccessTokenUseCase,
        [FromQuery] string code,
        [FromQuery] string state)
    {
        var useCaseResult = await getGovAccessTokenUseCase.Execute(code, state);
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
