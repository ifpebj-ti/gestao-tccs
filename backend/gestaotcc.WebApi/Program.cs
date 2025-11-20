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
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCorsExtension(builder.Configuration);
builder.Services.AddSwaggerExtension();
builder.Services.AddIocDependencies();
builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Host.AddSerilogExtension(builder.Configuration);
builder.Services.AddHangfireExtension(builder.Configuration);
builder.Services.AddOpenTelemetryExtension(builder.Environment, builder.Configuration);
builder.Services.AddMinioExtension(builder.Configuration, builder.Environment);

var app = builder.Build();

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
app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<LogMiddleware>();

app.MapPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();