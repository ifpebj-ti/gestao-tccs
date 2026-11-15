using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Infra.Database;
using gestaotcc.Infra.Gateways;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public sealed class ProposalPostgresFactAttribute : FactAttribute
{
    public ProposalPostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GRADUS_PROPOSAL_TEST_DATABASE")))
            Skip = "Configure GRADUS_PROPOSAL_TEST_DATABASE para um PostgreSQL temporário, conforme a documentação.";
    }
}

public class ProposalPostgresTests : IAsyncLifetime
{
    private string connectionString = "";
    private readonly string schema = "proposal_test_" + Guid.NewGuid().ToString("N");
    private long ownerId, otherOwnerId, guestId, advisorId;

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("GRADUS_PROPOSAL_TEST_DATABASE");
        if (raw == null) return;
        var settings = new NpgsqlConnectionStringBuilder(raw);
        // Reject application databases, even if a developer accidentally passes their normal connection string.
        if (settings.Database != "gradus_proposal_test" || settings.Host != "127.0.0.1" || settings.Port != 55439)
            throw new InvalidOperationException("Os testes exigem o banco temporário gradus_proposal_test em 127.0.0.1:55439.");
        settings.SearchPath = schema; connectionString = settings.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection); await command.ExecuteNonQueryAsync();
        await using var context = Context(); await context.Database.MigrateAsync();
        var student = new ProfileEntity { Role = "STUDENT" }; var advisor = new ProfileEntity { Role = "ADVISOR" };
        var course = new CampiCourseEntity { Campi = new() { Name = "Campus fictício", City = "Teste" }, Course = new() { Name = "Curso fictício", Level = "Superior" } };
        UserEntity User(string name, ProfileEntity profile) => new() { Name = name, Email = name + "@example.com", Status = "ACTIVE", CampiCourse = course, Profile = [profile] };
        var owner = User("owner", student); var other = User("other", student); var guest = User("guest", student); var teacher = User("teacher", advisor);
        guest.Email = "Guest@Example.com";
        context.Semesters.Add(new() { Name = "2026.2", IsActive = true, StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(3) });
        context.Users.AddRange(owner, other, guest, teacher); await context.SaveChangesAsync();
        ownerId = owner.Id; otherOwnerId = other.Id; guestId = guest.Id; advisorId = teacher.Id;
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(connectionString)) return;
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        // schema is generated locally from a fixed prefix and a GUID; never supplied by the caller.
        await using var command = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", connection); await command.ExecuteNonQueryAsync();
    }

    private SubmitTccProposalUseCase Submit(AppDbContext context) => new(new UserGateway(context), new TccGateway(context),
        Substitute.For<IProfileGateway>(), Substitute.For<IEmailGateway>(), new SemesterGateway(context), Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>(), new ProposalGateway(context));

    [ProposalPostgresFact]
    public async Task ConcurrentPasswordResetsConsumeTheCodeOnlyOnce()
    {
        long codeId;
        await using (var setup = Context())
        {
            var code = new gestaotcc.Domain.Entities.AccessCode.AccessCodeEntity
            { UserId = ownerId, Code = "reset-test", IsActive = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5) };
            setup.AccessCodes.Add(code); await setup.SaveChangesAsync(); codeId = code.Id;
        }
        async Task<bool> Reset(string hash)
        {
            await using var db = Context();
            return await new UserGateway(db).ResetPassword(ownerId, "", codeId, "reset-test", hash);
        }
        var results = await Task.WhenAll(Reset("first-hash"), Reset("second-hash"));
        Assert.Equal(1, results.Count(result => result));
        await using var verify = Context();
        Assert.True((await verify.AccessCodes.SingleAsync(code => code.Id == codeId)).IsUserUpdatePassword);
        var password = (await verify.Users.SingleAsync(user => user.Id == ownerId)).Password;
        Assert.Equal(results[0] ? "first-hash" : "second-hash", password);
    }

    [ProposalPostgresFact]
    public async Task FailedPasswordCompareRollsBackCodeConsumption()
    {
        await using var db = Context();
        var code = new gestaotcc.Domain.Entities.AccessCode.AccessCodeEntity
        { UserId = ownerId, Code = "reset-test", IsActive = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5) };
        db.AccessCodes.Add(code); await db.SaveChangesAsync();
        Assert.False(await new UserGateway(db).ResetPassword(ownerId, "stale-hash", code.Id, code.Code, "new-hash"));
        await using var verify = Context();
        Assert.False((await verify.AccessCodes.SingleAsync(item => item.Id == code.Id)).IsUserUpdatePassword);
        Assert.Equal("", (await verify.Users.SingleAsync(user => user.Id == ownerId)).Password);
        Assert.True(await new UserGateway(verify).ResetPassword(ownerId, "", code.Id, code.Code, "new-hash"));
    }

    [ProposalPostgresFact]
    public async Task ConcurrentFirstAccessRequestsCannotReuseTheInvitation()
    {
        long inviteId;
        await using (var setup = Context())
        {
            var user = await setup.Users.SingleAsync(item => item.Id == ownerId);
            var invite = new gestaotcc.Domain.Entities.TccInvite.TccInviteEntity
            { Email = user.Email, Code = "INV123", IsValidCode = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5) };
            setup.TccInvites.Add(invite); await setup.SaveChangesAsync(); inviteId = invite.Id;
        }
        async Task<bool> Complete(string hash)
        {
            await using var db = Context();
            return await new UserGateway(db).CompleteFirstAccess(ownerId, "", inviteId, "INV123", hash);
        }
        var results = await Task.WhenAll(Complete("first-hash"), Complete("second-hash"));
        Assert.Equal(1, results.Count(result => result));
        await using var verify = Context();
        Assert.True((await verify.TccInvites.SingleAsync(invite => invite.Id == inviteId)).IsValidCode);
    }

    [ProposalPostgresFact]
    public async Task GroupProposalKeepsActiveSemesterAndSemesterFilterAfterIntegration()
    {
        await using var context = Context();
        var semester = await context.Semesters.SingleAsync();
        var result = await Submit(context).Execute(new("Grupo integrado", "Resumo", advisorId, ["future@example.com"]), ownerId);
        Assert.True(result.IsSuccess);
        var proposal = await context.Tccs.SingleAsync();
        Assert.Equal(semester.Id, proposal.SemesterId);
        Assert.Equal("AWAITING_PARTICIPANTS", proposal.Status);
        var gateway = new TccGateway(context);
        Assert.Single(await gateway.FindAllTccByFilter(new(advisorId, "AWAITING_PARTICIPANTS", semester.Id), 0));
        Assert.Empty(await gateway.FindAllTccByFilter(new(advisorId, null, semester.Id + 1000), 0));
        Assert.Single(await gateway.FindAllTccByFilter(new(advisorId, null), 0));
        Assert.False((await Submit(context).Execute(new("Duplicada", "Resumo", advisorId), ownerId)).IsSuccess);

        semester = await context.Semesters.SingleAsync();
        semester.IsActive = false; await context.SaveChangesAsync();
        var withoutSemester = await Submit(context).Execute(new("Sem semestre", "Resumo", advisorId), otherOwnerId);
        Assert.False(withoutSemester.IsSuccess);
        Assert.Equal(422, withoutSemester.ErrorDetails!.Status);
        Assert.Equal(1, await context.Tccs.CountAsync());
    }

    private ProposalParticipantsUseCase Participants(AppDbContext context) => new(new ProposalGateway(context), new TccGateway(context), new UserGateway(context));

    [ProposalPostgresFact]
    public async Task MigrationsRoundTripAndModelStayConsistent()
    {
        await using var context = Context();
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Contains("20261013193257_AddProposalParticipants", await context.Database.GetAppliedMigrationsAsync());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261006182455_AddSemester");
        await migrator.MigrateAsync();
        Assert.Equal(0, await context.ProposalParticipants.CountAsync());
        Assert.Equal(4, await context.Users.CountAsync());
    }

    [ProposalPostgresFact]
    public async Task ConcurrentSubmissionsForSameOwnerProduceOnlyOneProposal()
    {
        await using var first = Context(); await using var second = Context();
        var results = await Task.WhenAll(Submit(first).Execute(new("Projeto A", "Resumo", advisorId), ownerId),
            Submit(second).Execute(new("Projeto B", "Resumo", advisorId), ownerId));
        Assert.Single(results.Where(x => x.IsSuccess)); Assert.Equal(409, results.Single(x => x.IsFailure).ErrorDetails?.Status);
        await using var verify = Context(); Assert.Equal(1, await verify.Tccs.CountAsync());
    }

    [ProposalPostgresFact]
    public async Task ConcurrentAcceptancesOfDifferentProposalsBindGuestOnlyOnce()
    {
        await using (var setup = Context())
        {
            Assert.True((await Submit(setup).Execute(new("Projeto A", "Resumo", advisorId, ["guest@example.com"]), ownerId)).IsSuccess);
        }
        await using (var setup = Context())
        {
            Assert.True((await Submit(setup).Execute(new("Projeto B", "Resumo", advisorId, ["guest@example.com"]), otherOwnerId)).IsSuccess);
        }
        List<long> invitationIds;
        await using (var read = Context()) invitationIds = await read.ProposalParticipants.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        await using var first = Context(); await using var second = Context();
        var results = await Task.WhenAll(Participants(first).Respond(invitationIds[0], guestId, true), Participants(second).Respond(invitationIds[1], guestId, true));
        Assert.Single(results.Where(x => x.IsSuccess)); Assert.Equal(409, results.Single(x => x.IsFailure).ErrorDetails?.Status);
        await using var verify = Context();
        Assert.Equal(1, await verify.UserTccs.CountAsync(x => x.UserId == guestId));
        Assert.Equal(1, await verify.ProposalParticipants.CountAsync(x => x.Status == "ACCEPTED"));
        Assert.Equal(1, await verify.ProposalParticipants.CountAsync(x => x.Status == "PENDING"));
    }

    [ProposalPostgresFact]
    public async Task PersistedInvitationIsListedAcceptedAndSharedWithOwner()
    {
        long tccId;
        await using (var submit = Context()) tccId = (await Submit(submit).Execute(new("Projeto", "Resumo", advisorId, ["guest@example.com"]), ownerId)).Data;
        long invitationId;
        await using (var read = Context())
        {
            var invitations = await Participants(read).List(guestId);
            var invitation = Assert.Single(invitations.Data); Assert.Equal("owner", invitation.RequestedBy); invitationId = invitation.Id;
            Assert.Equal(0, await read.UserTccs.CountAsync(x => x.UserId == guestId));
        }
        await using (var accept = Context()) Assert.True((await Participants(accept).Respond(invitationId, guestId, true)).IsSuccess);
        await using var verify = Context();
        Assert.Empty((await Participants(verify).List(guestId)).Data);
        var tcc = await new TccGateway(verify).FindTccById(tccId);
        Assert.Equal(2, tcc!.UserTccs.Count(x => x.Profile.Role == "STUDENT"));
        Assert.Equal("ACCEPTED", Assert.Single((await Participants(verify).Status(tccId, ownerId)).Data).Status);
        var types = Substitute.For<IDocumentTypeGateway>(); types.FindAll().Returns(new List<gestaotcc.Domain.Entities.DocumentType.DocumentTypeEntity>());
        var approve = new ApproveTccProposalUseCase(new TccGateway(verify), types, Substitute.For<IEmailGateway>(),
            Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>(), new ProposalGateway(verify));
        Assert.True((await approve.Execute(tccId, advisorId)).IsSuccess);
        await using var approved = Context();
        Assert.Equal("IN_PROGRESS", (await approved.Tccs.SingleAsync()).Status);
        Assert.Equal(2, await approved.UserTccs.CountAsync(x => x.Profile.Role == "STUDENT"));
    }

    [ProposalPostgresFact]
    public async Task FailedAtomicOperationRollsBackItsWrites()
    {
        await using var context = Context();
        var result = await new ProposalGateway(context).Atomic(async () =>
        {
            context.Tccs.Add(new() { Title = "Não persistir", Status = "PENDING_APPROVAL", Step = "PROPOSAL_REGISTRATION", CreationDate = DateTime.UtcNow });
            await context.SaveChangesAsync();
            return ResultPattern<string>.FailureResult("Falha de teste", 409);
        });
        Assert.False(result.IsSuccess);
        await using var verify = Context(); Assert.Equal(0, await verify.Tccs.CountAsync());
    }

    [ProposalPostgresFact]
    public async Task RegistrationAfterSubmissionKeepsConsentPendingUntilStudentAccepts()
    {
        await using var context = Context();
        var result = await Submit(context).Execute(new("Projeto futuro", "Resumo", advisorId, ["late@example.com"]), ownerId);
        Assert.True(result.IsSuccess);
        Assert.Equal("AWAITING_PARTICIPANTS", (await context.Tccs.SingleAsync()).Status);
        var invitation = await context.ProposalParticipants.SingleAsync(); Assert.Null(invitation.UserId);
        var userGateway = new UserGateway(context); var tccGateway = new TccGateway(context);
        var mail = Substitute.For<IEmailGateway>(); mail.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
        var send = new SendStudentVerificationCodeUseCase(userGateway, tccGateway, mail, Substitute.For<IAppLoggerGateway<SendStudentVerificationCodeUseCase>>());
        Assert.True((await send.Execute(new("late@example.com"))).IsSuccess);
        var verification = await context.TccInvites.SingleAsync(x => x.Email == "late@example.com");
        var verify = new VerifyCodeInviteTccUseCase(userGateway, tccGateway, Substitute.For<IAppLoggerGateway<VerifyCodeInviteTccUseCase>>());
        Assert.True((await verify.Execute(new("late@example.com", verification.Code))).IsSuccess);
        var course = await context.CampiCourses.Include(x => x.Course).Include(x => x.Campi).SingleAsync();
        var courses = Substitute.For<ICourseGateway>(); courses.FindByCampiAndCourseId(course.CampiId, course.CourseId).Returns(course);
        var studentProfiles = await context.Profiles.Where(x => x.Role == "STUDENT").ToListAsync();
        var profiles = Substitute.For<IProfileGateway>(); profiles.FindByRole(Arg.Any<List<string>>()).Returns(studentProfiles);
        var hash = Substitute.For<IBcryptGateway>(); hash.GenerateHashPassword(Arg.Any<string>()).Returns("hash-ficticio");
        var register = new gestaotcc.Application.UseCases.User.AutoRegisterUseCase(userGateway, tccGateway, profiles, courses, mail,
            Substitute.For<IDocumentTypeGateway>(), new gestaotcc.Application.UseCases.AccessCode.CreateAccessCodeUseCase(Substitute.For<IAppLoggerGateway<gestaotcc.Application.UseCases.AccessCode.CreateAccessCodeUseCase>>()),
            hash, Substitute.For<IAppLoggerGateway<gestaotcc.Application.UseCases.User.AutoRegisterUseCase>>());
        var student = await register.Execute(new("Aluno futuro", "late@example.com", "TEST-2026", "123.456.789-09", "81999999999", "Teste",
            gestaotcc.Domain.Enums.ShiftType.MORNING, course.CourseId, course.CampiId, "SenhaTeste123!"), "verificacao-ficticia");
        Assert.True(student.IsSuccess);
        Assert.Equal(0, await context.UserTccs.CountAsync(x => x.UserId == student.Data.Id));
        Assert.Empty(await tccGateway.FindAllTccByFilter(new(advisorId, "PENDING_APPROVAL"), 0));
        Assert.Equal(invitation.Id, Assert.Single((await Participants(context).List(student.Data.Id)).Data).Id);
        Assert.True((await Participants(context).Respond(invitation.Id, student.Data.Id, true)).IsSuccess);
        Assert.Single(await tccGateway.FindAllTccByFilter(new(advisorId, "PENDING_APPROVAL"), 0));
        Assert.Equal(student.Data.Id, (await context.ProposalParticipants.SingleAsync()).UserId);
    }

    [ProposalPostgresFact]
    public async Task DeclineNotificationIsPersistentAndDismissibleOnlyByOwner()
    {
        await using var context = Context();
        Assert.True((await Submit(context).Execute(new("Projeto", "Resumo", advisorId, ["guest@example.com"]), ownerId)).IsSuccess);
        var invitation = await context.ProposalParticipants.SingleAsync();
        Assert.True((await Participants(context).Respond(invitation.Id, guestId, false)).IsSuccess);
        await using var read = Context();
        Assert.Single((await Participants(read).Notifications(ownerId)).Data);
        Assert.Empty((await Participants(read).Notifications(guestId)).Data);
        Assert.Equal(404, (await Participants(read).Dismiss(invitation.Id, guestId)).ErrorDetails?.Status);
        Assert.True((await Participants(read).Dismiss(invitation.Id, ownerId)).IsSuccess);
        await using var after = Context(); Assert.Empty((await Participants(after).Notifications(ownerId)).Data);
        Assert.Equal("CANCELED", (await after.Tccs.SingleAsync()).Status);
    }

    [ProposalPostgresFact]
    public async Task EmailMigrationPreservesExistingInvitationsAndHidesThemUntilConsent()
    {
        await using var context = Context();
        var migrator = context.GetService<IMigrator>();
        var owner = await context.Users.Include(x => x.Profile).SingleAsync(x => x.Id == ownerId);
        var teacher = await context.Users.Include(x => x.Profile).SingleAsync(x => x.Id == advisorId);
        var legacy = new TccEntity { Title = "Proposta anterior", Status = "PENDING_APPROVAL", Step = "PROPOSAL_REGISTRATION", CreationDate = DateTime.UtcNow };
        legacy.UserTccs.Add(new() { User = owner, Profile = owner.Profile.First(), BindingDate = DateTime.UtcNow });
        legacy.UserTccs.Add(new() { User = teacher, Profile = teacher.Profile.First(), BindingDate = DateTime.UtcNow });
        context.Tccs.Add(legacy); await context.SaveChangesAsync();
        // The current model includes columns introduced after the invitation migration.
        // Persist the fixture before downgrading; insert legacy invitations using SQL below.
        await migrator.MigrateAsync("20261013193257_AddProposalParticipants");
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProposalParticipants" ("TccId", "UserId", "Status", "CreatedAt")
            VALUES ({legacy.Id}, {guestId}, {"PENDING"}, {DateTime.UtcNow}),
                   ({legacy.Id}, {otherOwnerId}, {"PENDING"}, {DateTime.UtcNow});
            """);
        await migrator.MigrateAsync();
        await using var verify = Context();
        Assert.Equal(2, await verify.ProposalParticipants.CountAsync());
        Assert.Equal(new[] { "guest@example.com", "other@example.com" }, await verify.ProposalParticipants.OrderBy(x => x.Email).Select(x => x.Email).ToArrayAsync());
        Assert.Equal("AWAITING_PARTICIPANTS", (await verify.Tccs.SingleAsync()).Status);
        Assert.Empty(await new TccGateway(verify).FindAllTccByFilter(new(advisorId, "PENDING_APPROVAL"), 0));
    }

    [ProposalPostgresFact]
    public async Task ProposalEntersAdvisorInboxOnlyAfterEveryInvitedStudentAccepts()
    {
        await using var context = Context();
        Assert.True((await Submit(context).Execute(new("Projeto de três", "Resumo", advisorId, ["guest@example.com", "other@example.com"]), ownerId)).IsSuccess);
        var invitations = await context.ProposalParticipants.ToListAsync();
        Assert.True((await Participants(context).Respond(invitations.Single(x => x.UserId == guestId).Id, guestId, true)).IsSuccess);
        Assert.Equal("AWAITING_PARTICIPANTS", (await context.Tccs.SingleAsync()).Status);
        Assert.Empty(await new TccGateway(context).FindAllTccByFilter(new(advisorId, "PENDING_APPROVAL"), 0));
        Assert.True((await Participants(context).Respond(invitations.Single(x => x.UserId == otherOwnerId).Id, otherOwnerId, true)).IsSuccess);
        Assert.Equal("PENDING_APPROVAL", (await context.Tccs.SingleAsync()).Status);
        Assert.Single(await new TccGateway(context).FindAllTccByFilter(new(advisorId, "PENDING_APPROVAL"), 0));
        Assert.Equal(3, await context.UserTccs.CountAsync(x => x.Profile.Role == "STUDENT"));
    }

    [ProposalPostgresFact]
    public async Task DowngradeRefusesToDiscardInvitationsWithoutAnAccount()
    {
        await using var context = Context();
        Assert.True((await Submit(context).Execute(new("Convite futuro", "Resumo", advisorId, ["future@example.com"]), ownerId)).IsSuccess);
        var error = await Assert.ThrowsAsync<PostgresException>(() => context.GetService<IMigrator>().MigrateAsync("20261013193257_AddProposalParticipants"));
        Assert.Equal("P0001", error.SqlState);
        Assert.Contains("convites por e-mail sem conta vinculada", error.MessageText);
        await using var verify = Context();
        var invitation = await verify.ProposalParticipants.SingleAsync();
        Assert.Equal("future@example.com", invitation.Email); Assert.Null(invitation.UserId);
        // Later migrations may already have been reverted before the invitation guard runs.
        // Read only a column belonging to the schema under test.
        Assert.Equal("AWAITING_PARTICIPANTS", await verify.Tccs.Select(x => x.Status).SingleAsync());
        Assert.Contains("20261013204914_AllowEmailProposalInvitations", await verify.Database.GetAppliedMigrationsAsync());
    }
}
