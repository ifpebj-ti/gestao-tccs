using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace gestaotcc.Test.Integration;

public class WebApiStartupTests
{
    [ProposalPostgresFact]
    public async Task ProductionPipelineStartsAndProtectsTheRealHttpRoutes()
    {
        var database = new CatalogPostgresTests();
        await database.InitializeAsync();
        try
        {
            await using var db = database.Context();
            var connection = db.Database.GetConnectionString();
            await using var app = Program.CreateApplication([
                "--environment=Production", "--urls=http://127.0.0.1:0",
                $"--ConnectionStrings:DefaultConnection={connection}",
                "--JWT_SETTINGS:TOKEN_KEY=synthetic-startup-tests-at-least-thirty-two-bytes",
                "--JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME=1",
                "--COMBINATION_STRING_FOR_ACCESSCODE=ABC234",
                "--STORAGE_SETTINGS:ENDPOINT=http://127.0.0.1:1",
                "--STORAGE_SETTINGS:ACCESS_KEY=synthetic",
                "--STORAGE_SETTINGS:SECRET_KEY=synthetic",
                "--STORAGE_SETTINGS:BUCKET_NAME=startup-test",
                "--CORS_SETTINGS:URL_FRONT=https://gradus.example.test",
                "--PublicApiUrl=https://api.gradus.example.test/api",
                "--URL_LOKI=http://127.0.0.1:1", "--URL_TEMPO=http://127.0.0.1:1/v1/traces"
            ]);
            await app.StartAsync();
            try
            {
                var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
                using var client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
                using var catalog = await client.GetAsync("/api/Campi/public/all");
                Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
                Assert.Equal("nosniff", catalog.Headers.GetValues("X-Content-Type-Options").Single());
                using var unknownHost = new HttpRequestMessage(HttpMethod.Get, "/api/Campi/public/all");
                unknownHost.Headers.Host = "unknown.example.test";
                using var refused = await client.SendAsync(unknownHost);
                Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Home")).StatusCode);

                using var login = await client.PostAsJsonAsync("/api/Auth/login", new
                {
                    email = "dev-test@ifpe.edu.br", password = "Senha@123"
                });
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                var session = await login.Content.ReadFromJsonAsync<JsonElement>();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Home")).StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Tcc", new
                {
                    students = Array.Empty<object>(), advisorId = 0
                })).StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Tcc/code/send", new
                {
                    userEmail = "invalid"
                })).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/Tcc/code/verify", new
                {
                    userEmail = "sem-convite@discente.ifpe.edu.br", code = "ABC234"
                })).StatusCode);
            }
            finally
            {
                await app.StopAsync();
            }
        }
        finally
        {
            await database.DisposeAsync();
        }
    }
}
