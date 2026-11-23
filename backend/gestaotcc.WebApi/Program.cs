using FluentValidation;
using gestaotcc.Infra.Database;
using gestaotcc.Infra.Database.PostgresSql;
using gestaotcc.Infra.Gateways;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Middlewares;
using gestaotcc.WebApi.SchemaFilters;
using gestaotcc.WebApi.Validators.User;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
var forwardedHeaders = TrustedProxy.FromConfiguration(builder.Configuration);
if (builder.Environment.IsProduction() && builder.Configuration["AllowedHosts"] is null or "" or "*" &&
    Uri.TryCreate(builder.Configuration["PublicApiUrl"], UriKind.Absolute, out var publicApi) &&
    publicApi.Scheme is "http" or "https")
{
    builder.Configuration["AllowedHosts"] = $"{publicApi.IdnHost};backend;localhost;127.0.0.1;[::1]";
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<FindAllByFilterValidator>();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<DateAndTimeOnlySchemaFilter>(); // Filtro de esquema personalizado para os tipos de dateOnly e TimeOnly
});

// Extension methods
builder.Services.AddDbContextExtension(builder.Configuration);
builder.Services.AddCorsExtension(builder.Configuration, builder.Environment);
builder.Services.AddSwaggerExtension();
builder.Services.AddIocDependencies();
builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Services.AddOwnPasswordRateLimit();
builder.Services.AddDataProtection();
builder.Services.AddScoped<PasswordResetSession>();
builder.Host.AddSerilogExtension(builder.Configuration);
builder.Services.AddHangfireExtension(builder.Configuration);
builder.Services.AddOpenTelemetryExtension(builder.Environment, builder.Configuration);
builder.Services.AddStorageExtension(builder.Configuration, builder.Environment);

var app = builder.Build();

if (forwardedHeaders is not null)
    app.UseForwardedHeaders(forwardedHeaders);

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Carregar migrations e Seeds
Log.Information("Executando Migrations");
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();
DbInitializer.Initialize(dbContext, new BcryptGateway());

app.UseHangfireExtension(app.Services);

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

// Middlewares
app.UseMiddleware<LogMiddleware>();

app.MapPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
