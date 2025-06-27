using gestaotcc.Infra.Database;
using gestaotcc.Infra.Database.PostgresSql;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Middlewares;
using gestaotcc.WebApi.SchemaFilters;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<DateAndTimeOnlySchemaFilter>(); // Filtro de esquema personalizado para os tipos de dateOnly e TimeOnly
});

// Extension methods
builder.Services.AddDbContextExtension(builder.Configuration);
builder.Services.AddCorsExtension();
builder.Services.AddSwaggerExtension();
builder.Services.AddIocDependencies();
builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Host.AddSerilogExtension();
builder.Services.AddHangfireExtension(builder.Configuration);
builder.Services.AddOpenTelemetryExtension(builder.Environment);
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
DbInitializer.Initialize(dbContext);

app.UseHangfireExtension(app.Services);

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

// Middlewares
app.UseMiddleware<LogMiddleware>();

app.MapPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();