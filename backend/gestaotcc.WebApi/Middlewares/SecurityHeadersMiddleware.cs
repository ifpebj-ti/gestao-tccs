using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace gestaotcc.WebApi.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Define as políticas de segurança
        
        // Evita ataques de Clickjacking garantindo que a API não possa ser embutida em iframes de outros domínios
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        
        // Previne ataques de XSS forçando o navegador a bloquear a resposta se um script suspeito for detectado
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Cache-Control"] = "no-store";
        
        // Evita que o navegador tente adivinhar o tipo de conteúdo (MIME sniffing)
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        // Política rigorosa para controlar recursos. Sendo uma API, não devemos aceitar recursos externos sendo carregados 
        // e permitimos apenas requests locais.
        var swaggerInDevelopment = context.Request.Path.StartsWithSegments("/swagger") &&
            context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;
        context.Response.Headers["Content-Security-Policy"] = swaggerInDevelopment
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'"
            : "default-src 'self'; frame-ancestors 'none'; base-uri 'none'; object-src 'none'";

        // HSTS - Informa aos navegadores para acessar este servidor apenas via HTTPS
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        await _next(context);
    }
}
