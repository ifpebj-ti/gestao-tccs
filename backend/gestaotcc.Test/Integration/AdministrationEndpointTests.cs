using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Errors;
using gestaotcc.Infra.Database.PostgresSql;
using gestaotcc.Infra.Gateways;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Controllers;
using gestaotcc.WebApi.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace gestaotcc.Test.Integration;

// HTTP, autenticação JWT, controllers, use cases e SQL reais; e-mail e S3 não saem do processo.
public class AdministrationEndpointTests : IAsyncLifetime
{
    private readonly CatalogPostgresTests database = new();
    private WebApplication app = null!;
    private HttpClient client = null!;
    private long adminId, studentId;
    private IConfiguration configuration = null!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GRADUS_PROPOSAL_TEST_DATABASE"))) return;
        await database.InitializeAsync();
        await using (var db = database.Context())
        {
            DbInitializer.Initialize(db, new BcryptGateway());
            adminId = (await db.Users.SingleAsync(u => u.Email == "dev-test@ifpe.edu.br")).Id;
            studentId = (await db.Users.SingleAsync(u => u.Email == "aluna@discente.ifpe.edu.br")).Id;
        }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT_SETTINGS:TOKEN_KEY"] = "synthetic-integration-key-at-least-32-bytes",
            ["JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME"] = "1",
            ["COMBINATION_STRING_FOR_ACCESSCODE"] = "ABC234",
            ["STORAGE_SETTINGS:BUCKET_NAME"] = "test-only"
        });
        configuration = builder.Configuration;
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddScoped(_ => database.Context());
        builder.Services.AddIocDependencies();
        var email = Substitute.For<IEmailGateway>();
        email.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
        builder.Services.AddSingleton(email);
        builder.Services.AddSingleton(Substitute.For<IStorageGateway>());
        builder.Services.AddSingleton(Substitute.For<IAmazonS3>());
        builder.Services.AddSingleton(Substitute.For<Hangfire.IBackgroundJobClient>());
        builder.Services.AddAuthenticationExtension(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddOwnPasswordRateLimit();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddScoped<PasswordResetSession>();
        builder.Services.AddControllers().AddApplicationPart(typeof(UserController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerExtension();
        app = builder.Build();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<GlobalExceptionHandler>();
        app.UseSwagger(); // Somente este host de teste expõe o contrato OpenAPI em Production.
        app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter(); app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (app != null) await app.DisposeAsync();
        await database.DisposeAsync();
    }

    private async Task Authenticate(long id)
    {
        await using var db = database.Context();
        var user = await new UserGateway(db).FindById(id);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new TokenGateway(configuration).CreateAccessToken(user!));
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [ProposalPostgresFact]
    public async Task CatalogReadsRequireTheCorrectRolesAndPublicCatalogRemainsAvailable()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Campi/all")).StatusCode);
        Assert.NotEmpty((await Json(await client.GetAsync("/api/Campi/public/all"))).EnumerateArray());
        Assert.Equal(22, (await Json(await client.GetAsync("/api/Campi/public/courses"))).GetArrayLength());
        await Authenticate(studentId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Campi/all")).StatusCode);
        Assert.NotEmpty((await Json(await client.GetAsync("/api/Campi/all/courses"))).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Campi", new { name = "Tentativa", city = "Teste" })).StatusCode);
        await Authenticate(adminId);
        Assert.Equal(11, (await Json(await client.GetAsync("/api/Campi/all"))).GetArrayLength());
        Assert.Equal(7, (await Json(await client.GetAsync("/api/Profile"))).GetArrayLength());
    }

    [ProposalPostgresFact]
    public async Task AdminCanManageCampusesCoursesAndSemestersThroughHttp()
    {
        await Authenticate(adminId);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Campi", new { name = "Campus HTTP", city = "Cidade" })).StatusCode);
        long campusId;
        await using (var db = database.Context()) campusId = (await db.Campi.SingleAsync(c => c.Name == "Campus HTTP")).Id;
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/Campi/{campusId}", new { name = "Campus editado", city = "Outra cidade" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Course", new { name = "Curso HTTP", level = "Superior", campiId = campusId })).StatusCode);
        long courseId;
        await using (var db = database.Context()) courseId = (await db.Courses.SingleAsync(c => c.Name == "Curso HTTP")).Id;
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/Course/{courseId}", new { name = "Curso editado", level = "Tecnólogo", campiId = campusId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/Course/{courseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/Campi/{campusId}")).StatusCode);
        var semester = new { name = "2026.2", startDate = "2026-07-01T00:00:00Z", endDate = "2026-12-31T00:00:00Z", isActive = true };
        var created = await Json(await client.PostAsJsonAsync("/api/Semester", semester));
        var semesterId = created.GetProperty("id").GetInt64();
        Assert.Single((await Json(await client.GetAsync("/api/Semester/all"))).EnumerateArray());
        Assert.Single((await Json(await client.GetAsync("/api/Semester/active"))).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/Semester/{semesterId}", semester)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/Semester/{semesterId}")).StatusCode);
        Assert.Equal(0, (await Json(await client.GetAsync("/api/Semester/active"))).GetArrayLength());
    }

    [ProposalPostgresFact]
    public async Task UserReadsMapPublicFieldsWithoutPasswordAndRevocationTakesEffectImmediately()
    {
        await Authenticate(adminId);
        var response = await client.GetAsync($"/api/User/{studentId}");
        var user = await Json(response);
        Assert.Equal(studentId, user.GetProperty("id").GetInt64());
        Assert.True(!user.TryGetProperty("password", out var password) || password.ValueKind == JsonValueKind.Null || password.GetString() == "");
        Assert.False(user.TryGetProperty("accessCode", out _));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/User/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User/all?pageNumber=1&pageSize=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User/filter?profile=STUDENT")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User/available-advisors/22")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User/email?email=aluna%40discente.ifpe.edu.br")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/User/email?email=missing%40example.com")).StatusCode);
        await using (var db = database.Context())
        {
            var admin = await db.Users.SingleAsync(u => u.Id == adminId);
            admin.Password = "changed-hash"; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/all")).StatusCode);
    }

    [ProposalPostgresFact]
    public async Task ProposalCanBeRejectedReformulatedApprovedAndUploadedWithRoleChecks()
    {
        long advisorId;
        await using (var db = database.Context())
        {
            var teacher = new gestaotcc.Domain.Entities.User.UserEntity
            { Name = "Docente sintético", Email = "docente@teste.ifpe.edu.br", Status = "ACTIVE", Password = "test-hash",
                CampiCourseId = 22, Profile = [await db.Profiles.SingleAsync(p => p.Role == "ADVISOR")] };
            db.Users.Add(teacher);
            db.Semesters.Add(new() { Name = "2026.2", IsActive = true, StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(1) });
            await db.SaveChangesAsync(); advisorId = teacher.Id;
        }
        await Authenticate(studentId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Tcc/proposal", new { title = "", summary = "", advisorId })).StatusCode);
        var proposal = new { title = "Segurança do fluxo de TCC", summary = "Proposta sintética para verificar a integração HTTP e a autorização.", advisorId };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Tcc/proposal", proposal)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Tcc/proposal", proposal)).StatusCode);
        long tccId;
        await using (var db = database.Context()) tccId = (await db.Tccs.SingleAsync()).Id;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/Tcc?tccId={tccId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/Tcc/workflow?tccId={tccId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/Tcc/{tccId}/approve-proposal", new { })).StatusCode);
        await Authenticate(advisorId);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/Tcc/{tccId}/reject-proposal", new { reason = "Detalhar a metodologia antes de aprovar a proposta." })).StatusCode);
        await Authenticate(studentId);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/Tcc/{tccId}/reformulate-proposal", proposal)).StatusCode);
        await Authenticate(advisorId);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/Tcc/{tccId}/approve-proposal", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/Tcc/{tccId}/approve-proposal", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/Tcc/{tccId}", new { title = "Título revisado", summary = "Resumo revisado pelo orientador" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Tcc/filter")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/api/Tcc/advisor-change?tccId={tccId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/Signature/pending?userId={advisorId}")).StatusCode);
        await Authenticate(studentId);
        using (var invalid = new MultipartFormDataContent())
        {
            var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<script>invalid</script>"));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); invalid.Add(content, "file", "fake.pdf");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/Tcc/{tccId}/upload-file", invalid)).StatusCode);
        }
        using (var valid = new MultipartFormDataContent())
        {
            var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("%PDF-1.7\nsynthetic\n%%EOF\n"));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); valid.Add(content, "file", "monografia.pdf");
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/Tcc/{tccId}/upload-file", valid)).StatusCode);
        }
        await using (var db = database.Context())
        {
            var tcc = await db.Tccs.SingleAsync();
            Assert.Equal("IN_PROGRESS", tcc.Status);
            Assert.StartsWith($"filled/tcc_{tccId}_", tcc.TccFile);
            Assert.NotEmpty(await db.Documents.Where(d => d.TccId == tccId).ToListAsync());
        }
    }

    [ProposalPostgresFact]
    public async Task AdministrativeUserCreationAndUpdateValidateInputAndPersistOnlyValidChanges()
    {
        await Authenticate(adminId);
        var user = new { name = "Pessoa cadastrada", email = "cadastro@discente.ifpe.edu.br", registration = "2026001",
            cpf = "123.456.789-09", siape = "12345", profile = new[] { "STUDENT" }, phone = "81999991234",
            userClass = "2026", shift = 1, titration = "Graduação", courseId = 22, campusId = 11 };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/User", user with { name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/User", user)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/User", user)).StatusCode);
        long id;
        await using (var db = database.Context()) id = (await db.Users.SingleAsync(u => u.Email == user.email)).Id;
        var update = new { id, name = "Pessoa atualizada", user.email, user.registration, user.cpf, user.siape, user.phone,
            user.userClass, user.shift, user.titration, user.profile, status = "ACTIVE", campiId = 11, courseId = 22 };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/User", update with { status = "INVALID" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/User", update)).StatusCode);
        var result = await Json(await client.GetAsync($"/api/User/{id}"));
        Assert.Equal("Pessoa atualizada", result.GetProperty("name").GetString());
        Assert.Equal("ACTIVE", result.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/api/User", update with { id = 999999L })).StatusCode);
    }

    [ProposalPostgresFact]
    public async Task RegistrationRequiresVerifiedInvitationAndLoginRejectsInvalidCredentials()
    {
        const string email = "novocadastro@discente.ifpe.edu.br";
        var registration = new { name = "Novo estudante", email, registration = "2026002", cpf = "123.456.789-09",
            phone = "81999991234", userClass = "2026", shift = 1, courseId = 22, campiId = 11, password = "Synthetic!234" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/User/autoregister", registration with { name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/User/autoregister", registration)).StatusCode);
        await using (var db = database.Context())
        {
            Assert.False(await db.Users.AnyAsync(u => u.Email == email));
            db.TccInvites.Add(new() { Email = email, Code = "ABC234", IsValidCode = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5), CampiId = 11, CourseId = 22 });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/User/autoregister", registration)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/User/autoregister", registration)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Auth/login", new { email = "invalid", password = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Auth/login", new { email, password = "Wrong!234" })).StatusCode);
        var login = await Json(await client.PostAsJsonAsync("/api/Auth/login", new { email, password = registration.password }));
        Assert.False(string.IsNullOrWhiteSpace(login.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Auth/banking-login", new { token = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Auth/banking-login", new { token = "invalid-token" })).StatusCode);
    }

    [ProposalPostgresFact]
    public async Task OpenApiIncludesAdministrativeAndProposalRoutesAndBearerAuthentication()
    {
        var document = await Json(await client.GetAsync("/swagger/v1/swagger.json"));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/Tcc/proposal", out _));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/User", out _));
        Assert.Equal("Authorization", document.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer").GetProperty("name").GetString());
        Assert.Contains("object-src 'none'", (await client.GetAsync("/swagger/v1/swagger.json")).Headers.GetValues("Content-Security-Policy").Single());
    }

    [ProposalPostgresFact]
    public async Task MissingTccResourcesReturnDocumentedFailuresWithoutWritingData()
    {
        // Perfis sintéticos para exercitar os adaptadores HTTP; o JWT ainda é validado contra o banco.
        await using (var db = database.Context())
        {
            var admin = await db.Users.Include(u => u.Profile).SingleAsync(u => u.Id == adminId);
            admin.Profile = await db.Profiles.ToListAsync(); await db.SaveChangesAsync();
        }
        await Authenticate(adminId);
        async Task Check(HttpMethod method, string path, object? body, HttpStatusCode expected)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request);
            Assert.True(response.StatusCode == expected, $"{method} {path}: esperado {(int)expected}; recebido {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
        var reason = new { reason = "Justificativa sintética com mais de vinte caracteres." };
        await Check(HttpMethod.Get, "/api/Tcc?tccId=999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Tcc/cancellation?tccId=999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Tcc/999999/schedule/calendar.ics", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/cancellation/approve?tccId=999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/cancellation/request", new { reason.reason, idTcc = 999999 }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/999999/conclude-presentation", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/schedule/email?tccId=999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/approve-proposal", new { }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/reject-proposal", reason, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/reformulate-proposal", new { title = "Proposta sintética", summary = "Resumo sintético" }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/cancel-proposal", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/confirm-hardcover", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/forward-library", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/confirm-library", new { }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Tcc/999999/final-file/download", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/999999/request-advisor-departure", new { reason.reason, suggestedAdvisorId = studentId }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Post, "/api/Tcc/advisor-change/request", new { tccId = 999999, reason.reason, requesterRole = "ADVISOR", suggestedAdvisorId = studentId }, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/advisor-change/approve", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Patch, "/api/Tcc/999999/advisor-change/reject", new { reviewNotes = reason.reason }, HttpStatusCode.NotFound);
        var schedule = new gestaotcc.Domain.Dtos.Tcc.ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today.AddDays(10)), new TimeOnly(10, 0), "Sala de teste", 999999,
            [new("Docente interno", "internal@example.com", "BANKING", studentId), new("Docente externo", "external@example.com", "BANKING")]);
        await Check(HttpMethod.Post, "/api/Tcc/schedule", schedule, HttpStatusCode.NotFound);
        await Check(HttpMethod.Put, "/api/Tcc/schedule", schedule, HttpStatusCode.NotFound);
        await Check(HttpMethod.Put, "/api/Tcc/999999", new { title = "Título revisado", summary = "Resumo válido" }, HttpStatusCode.InternalServerError);
        await Check(HttpMethod.Get, "/api/Signature/document?tccId=999999&documentId=999999&studentId=0", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Signature/document/download?tccId=999999&documentId=999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Signature/all/documents/download/999999", null, HttpStatusCode.NotFound);
        await Check(HttpMethod.Get, "/api/Tcc/library-pending", null, HttpStatusCode.OK);
        await Check(HttpMethod.Get, "/api/Tcc/coordination-pending", null, HttpStatusCode.OK);
        await Check(HttpMethod.Get, "/api/Tcc/banking/pending-evaluations", null, HttpStatusCode.OK);
        await using var verify = database.Context();
        Assert.Empty(await verify.Tccs.ToListAsync());
        Assert.Empty(await verify.Documents.ToListAsync());
    }
}
