using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.WebApi.ResponseModels;
using gestaotcc.WebApi.ResponseModels.Auth;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Validators;
using gestaotcc.WebApi.Validators.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
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

    /// <summary>
    /// Inicia a autenticação OpenID Connect do Google.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("google")]
    public IActionResult GoogleLogin([FromServices] IConfiguration configuration)
    {
        var google = configuration.GetSection("GoogleAuth");
        var redirectUriValue = google["RedirectUri"];
        var frontendUrl = configuration["CORS_SETTINGS:URL_FRONT"];
        var redirectUriIsValid = Uri.TryCreate(redirectUriValue, UriKind.Absolute, out var redirectUri)
            && (redirectUri.Scheme == Uri.UriSchemeHttps || redirectUri.IsLoopback && redirectUri.Scheme == Uri.UriSchemeHttp)
            && string.IsNullOrEmpty(redirectUri.Query)
            && string.IsNullOrEmpty(redirectUri.Fragment);
        var frontendUriIsValid = Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri)
            && (frontendUri.Scheme == Uri.UriSchemeHttps || frontendUri.Scheme == Uri.UriSchemeHttp);
        var googleConfigured = !string.IsNullOrWhiteSpace(google["ClientId"])
            && !string.IsNullOrWhiteSpace(google["ClientSecret"])
            && redirectUriIsValid
            && frontendUriIsValid;

        if (!googleConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Login com Google indisponível neste ambiente." });

        var completionPath = Url.Action(nameof(GoogleLoginComplete), "Auth");
        if (string.IsNullOrWhiteSpace(completionPath))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Não foi possível iniciar o login com o Google." });

        return Challenge(new AuthenticationProperties { RedirectUri = completionPath },
            GoogleAuthenticationDefaults.Scheme);
    }

    /// <summary>
    /// Finaliza o callback OIDC, aplica a elegibilidade local e emite o JWT do sistema.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("google/complete")]
    public async Task<IActionResult> GoogleLoginComplete(
        [FromServices] GoogleLoginUseCase googleLoginUseCase,
        [FromServices] IConfiguration configuration)
    {
        var ticket = await HttpContext.AuthenticateAsync(GoogleAuthenticationDefaults.ExternalCookieScheme);
        if (!ticket.Succeeded || ticket.Principal is null)
        {
            await HttpContext.SignOutAsync(GoogleAuthenticationDefaults.ExternalCookieScheme);
            return Unauthorized(new { message = "Não foi possível autenticar esta conta com o Google." });
        }

        var principal = ticket.Principal;
        var email = principal.FindFirst("email")?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? string.Empty;
        var emailVerifiedValue = principal.FindFirst("email_verified")?.Value;
        var emailVerified = bool.TryParse(emailVerifiedValue, out var verified) && verified;
        var allowedDomains = configuration["GoogleAuth:AllowedDomains"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        var result = await googleLoginUseCase.Execute(
            new GoogleIdentityDTO(email, emailVerified), allowedDomains);
        await HttpContext.SignOutAsync(GoogleAuthenticationDefaults.ExternalCookieScheme);

        if (result.IsFailure)
            return Unauthorized(new { message = "Não foi possível autenticar esta conta com o Google." });

        var frontendUrl = configuration["CORS_SETTINGS:URL_FRONT"];
        if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri) ||
            (frontendUri.Scheme != Uri.UriSchemeHttps && frontendUri.Scheme != Uri.UriSchemeHttp))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Não foi possível concluir o login com o Google." });
        }

        var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var isHttps = Request.IsHttps || string.Equals(forwardedProto, "https", StringComparison.OrdinalIgnoreCase);
        Response.Cookies.Append(GoogleAuthenticationDefaults.LocalJwtCookieName, result.Data.AccessToken,
            new CookieOptions
            {
                Path = "/",
                HttpOnly = false,
                Secure = isHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });

        return Redirect(frontendUri.AbsoluteUri);
    }
    /// <summary>
    /// Realiza login no sistema para avaliador externo
    /// </summary>
    [AllowAnonymous]
    [HttpPost("banking-login")]
    public async Task<ActionResult<LoginResponseModel>> BankingLogin([FromQuery] string token, [FromServices] BankingLoginUseCase bankingLoginUseCase)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token não informado." });

        var useCaseResult = await bankingLoginUseCase.Execute(token);

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
