using gestaotcc.Infra.Database;
using gestaotcc.Infra.Database.PostgresSql;
using gestaotcc.Infra.Gateways;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Semester;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace gestaotcc.Test.Integration;

// Executa apenas no PostgreSQL descartável, em um schema exclusivo por teste.
public class CatalogPostgresTests : IAsyncLifetime
{
    private readonly string schema = "catalog_test_" + Guid.NewGuid().ToString("N");
    private string connectionString = "";
    internal AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("GRADUS_PROPOSAL_TEST_DATABASE");
        if (raw == null) return;
        var settings = new NpgsqlConnectionStringBuilder(raw);
        if (settings.Database != "gradus_proposal_test" || settings.Host != "127.0.0.1" || settings.Port != 55439)
            throw new InvalidOperationException("Use somente o PostgreSQL temporário de testes.");
        settings.SearchPath = schema; connectionString = settings.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
        await command.ExecuteNonQueryAsync();
        await using var db = Context(); await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (connectionString == "") return;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    [ProposalPostgresFact]
    public async Task InitializationIsIdempotentAndPreservesExistingAccounts()
    {
        await using var db = Context();
        var bcrypt = new BcryptGateway();
        DbInitializer.Initialize(db, bcrypt);
        var original = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
        var documents = await db.DocumentTypes.CountAsync();
        Assert.Equal(7, await db.Profiles.CountAsync());
        Assert.Equal(11, await db.Campi.CountAsync());
        Assert.Equal(22, await db.Courses.CountAsync());
        Assert.Equal(2, original.Count);
        Assert.All(original, user => Assert.NotEqual("Senha@123", user.Password));
        var admin = await db.Users.Include(u => u.Profile).SingleAsync(u => u.Email == "dev-test@ifpe.edu.br");
        Assert.Contains(admin.Profile, p => p.Role == "ADMIN");
        admin.Name = "Nome alterado pelo usuário";
        await db.SaveChangesAsync();
        DbInitializer.Initialize(db, bcrypt);
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(documents, await db.DocumentTypes.CountAsync());
        Assert.Equal("Nome alterado pelo usuário", (await db.Users.SingleAsync(u => u.Id == admin.Id)).Name);
        Assert.Equal(original.Select(u => u.Password), await db.Users.OrderBy(u => u.Id).Select(u => u.Password).ToListAsync());
        // Sequences continuam válidas após os IDs explícitos da carga inicial.
        var campus = new CampiEntity { Name = "Campus adicional", City = "Teste" };
        db.Campi.Add(campus); await db.SaveChangesAsync();
        Assert.True(campus.Id > 11);
    }

    [ProposalPostgresFact]
    public async Task CourseAndCampusCrudPersistsLinksAndUpdatesWithoutChangingOtherCampuses()
    {
        await using var db = Context(); var gateway = new CourseGateway(db);
        var campus = await gateway.InsertCampi(new CampiEntity { Name = "Campus A", City = "Cidade A" });
        var other = await gateway.InsertCampi(new CampiEntity { Name = "Campus B", City = "Cidade B" });
        var course = await gateway.InsertCourse(new CourseEntity { Name = "Curso A", Level = "Superior" }, campus.Id);
        var linked = await gateway.FindByCampiAndCourseId(campus.Id, course.Id);
        Assert.NotNull(linked);
        Assert.Equal(course.Id, (await gateway.FindByName("Curso A")).Id);
        Assert.Equal(course.Id, (await gateway.FindAllCoursesByCampiCourseId(linked.Id)).Single().Id);
        Assert.Empty((await gateway.FindAllCampis()).Single(c => c.Id == other.Id).CampiCourses);
        campus.City = "Cidade atualizada"; await gateway.UpdateCampi(campus);
        course.Level = "Tecnólogo"; await gateway.UpdateCourse(course);
        Assert.Equal("Cidade atualizada", (await gateway.FindCampiById(campus.Id)).City);
        Assert.Equal("Tecnólogo", (await gateway.FindCourseById(course.Id)).Level);
        await gateway.DeleteCourse(course.Id); await gateway.DeleteCampi(campus.Id);
        Assert.Equal(other.Id, (await gateway.FindAllCampis()).Single().Id);
        await Assert.ThrowsAsync<Exception>(() => gateway.FindCourseById(course.Id));
        await Assert.ThrowsAsync<Exception>(() => gateway.FindCampiById(campus.Id));
        await Assert.ThrowsAsync<Exception>(() => gateway.FindByName("Curso inexistente"));
    }

    [ProposalPostgresFact]
    public async Task SemestersPersistActivityOrderingAndDeletion()
    {
        await using var db = Context(); var gateway = new SemesterGateway(db);
        var previous = await gateway.Insert(new SemesterEntity { Name = "2026.1", IsActive = false,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc) });
        var current = await gateway.Insert(new SemesterEntity { Name = "2026.2", IsActive = true,
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc) });
        Assert.Equal(new[] { current.Id, previous.Id }, (await gateway.FindAll()).Select(s => s.Id));
        Assert.Equal(current.Id, (await gateway.FindActive()).Single().Id);
        current.IsActive = false; await gateway.Update(current);
        Assert.Empty(await gateway.FindActive());
        Assert.False((await gateway.FindById(current.Id))!.IsActive);
        await gateway.Delete(previous.Id); await gateway.Delete(previous.Id);
        Assert.Null(await gateway.FindById(previous.Id));
        Assert.Single(await gateway.FindAll());
    }

    [ProposalPostgresFact]
    public async Task EmptyDatabaseMigrationRoundTripRecreatesAUsableCatalog()
    {
        // Valida a reversibilidade do esquema vazio; nunca aplica downgrade a dados da aplicação.
        await using var db = Context();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotEmpty(migrations);
        await db.GetService<IMigrator>().MigrateAsync("0");
        Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
        await db.Database.MigrateAsync();
        Assert.Equal(migrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        DbInitializer.Initialize(db, new BcryptGateway());
        Assert.Equal(22, await db.Courses.CountAsync());
        Assert.Equal(7, await db.Profiles.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.NotEmpty(await db.DocumentTypes.ToListAsync());
    }
}
