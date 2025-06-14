using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();

// Extension methods
builder.Services.AddDbContextExtension(builder.Configuration);
builder.Services.AddCorsExtension();
builder.Services.AddSwaggerExtension();
builder.Services.AddIocDependencies();
builder.Services.AddAuthenticationExtension(builder.Configuration);
builder.Host.AddSerilogExtension();
builder.Services.AddHangfireExtension(builder.Configuration);
builder.Services.AddOpenTelemetryExtension(builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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