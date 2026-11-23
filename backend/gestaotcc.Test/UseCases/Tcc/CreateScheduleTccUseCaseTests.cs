using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using NSubstitute;
using Hangfire;

namespace gestaotcc.Test.UseCases.Tcc;

public class CreateScheduleTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly CreateScheduleTccUseCase _useCase;
    private readonly IAppLoggerGateway<CreateScheduleTccUseCase> _logger = Substitute.For<IAppLoggerGateway<CreateScheduleTccUseCase>>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();

    public CreateScheduleTccUseCaseTests()
    {
        _useCase = new CreateScheduleTccUseCase(_tccGateway, _userGateway, _profileGateway, _logger, _emailGateway, _backgroundJobClient, _storageGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns((TccEntity?)null);

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), "Sala 101", 1);
        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldUpdate_WhenTccAlreadyHasSchedule()
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, TccSchedule = new TccScheduleEntity() });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0), "Sala 202", 1);
        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        Assert.Equal("Sala 202", tcc.TccSchedule.Location);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenScheduleIsCreated()
    {
        // Arrange
        var scheduleDate = DateOnly.FromDateTime(DateTime.Today);
        var scheduleTime = new TimeOnly(9, 30);
        var location = "Auditório";

        var tcc = AdvisorTcc(new TccEntity { Id = 1, TccSchedule = null, Step = gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString() });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(scheduleDate, scheduleTime, location, 1);

        // Act
        var result = await _useCase.Execute(dto, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);

        // Verifica se o agendamento foi criado corretamente
        Assert.NotNull(tcc.TccSchedule);
        Assert.Equal(location, tcc.TccSchedule.Location);
        Assert.Equal(1, tcc.TccSchedule.TccId);

        var expectedDateTime = DateTime.SpecifyKind(scheduleDate.ToDateTime(scheduleTime), DateTimeKind.Utc);
        Assert.Equal(expectedDateTime, tcc.TccSchedule.ScheduledDate);

        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldRejectBankingExaminer_WhenUserIsNotTccAdvisor()
    {
        var tcc = new TccEntity
        {
            Id = 1,
            TccSchedule = null,
            Step = gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString(),
            UserTccs =
            {
                new UserTccEntity { UserId = 2, Profile = new ProfileEntity { Role = "BANKING" } }
            }
        };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 30), "Sala 1", 1);
        var result = await _useCase.Execute(dto, 2);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldPreserveExternalInvite_WhenEmailMatchesRegisteredAccount()
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, Step = gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString() });
        var registeredUser = new UserEntity
        {
            Id = 22,
            Name = "Examinador Cadastrado",
            Email = "examiner@example.com",
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADVISOR" } },
            UserTccs = new List<UserTccEntity>()
        };
        var bankingProfile = new ProfileEntity { Id = 9, Role = "BANKING" };
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _userGateway.FindByEmail("examiner@example.com").Returns(registeredUser);
        _profileGateway.FindByRole("BANKING").Returns(bankingProfile);
        _emailGateway.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>())
            .Returns(Task.FromResult(gestaotcc.Domain.Errors.ResultPattern<bool>.SuccessResult(true)));

        var dto = new ScheduleTccDTO(
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 30), "Sala 1", 1,
            new[] { new BankingMemberDto("Nome digitado", "examiner@example.com", "Examinador Externo") });

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        var member = Assert.Single(tcc.BankingMembers.Where(member => member.Email == registeredUser.Email));
        Assert.Null(member.UserId);
        Assert.Equal("Examinador Externo", member.Role);
        await _emailGateway.Received(1).Send(Arg.Is<gestaotcc.Domain.Dtos.Email.SendEmailDTO>(email => email.TypeTemplate == "BANKING-INVITE"));
    }

    [Fact]
    public async Task Execute_ShouldRejectSameRegisteredExaminerSubmittedTwice()
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, Step = gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString() });
        var registeredUser = new UserEntity
        {
            Id = 22,
            Name = "Examinador Cadastrado",
            Email = "examiner@example.com",
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADVISOR" } }
        };
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _userGateway.FindById(22).Returns(registeredUser);
        _userGateway.FindByEmail("examiner@example.com").Returns(registeredUser);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 9, Role = "BANKING" });

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 30), "Sala 1", 1, new[]
        {
            new BankingMemberDto(registeredUser.Name, registeredUser.Email, "Examinador Interno", registeredUser.Id),
            new BankingMemberDto(registeredUser.Name, registeredUser.Email, "Examinador Externo")
        });

        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnServerError_WhenUpdateFails()
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, TccSchedule = null, Step = gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString() });
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _tccGateway.When(g => g.Update(tcc)).Do(x => throw new Exception("DB Error"));

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(15, 0), "Sala 303", 1);
        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao criar agendamento de defesa do TCC", result.Message);
    }

    [Theory]
    [InlineData(null, "ACTIVE", "ADVISOR", false)]
    [InlineData(22, "INACTIVE", "ADVISOR", false)]
    [InlineData(22, "ACTIVE", null, false)]
    [InlineData(22, "ACTIVE", "STUDENT", false)]
    [InlineData(22, "INACTIVE", "ADVISOR", true)]
    public async Task Execute_RejectsIneligibleExaminerBeforeSaving(int? accountId, string status, string? role, bool lookupByEmail)
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, Step = "PRESENTATION_AND_EVALUATION" });
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        UserEntity? account = accountId is null ? null : new UserEntity
        {
            Id = accountId.Value, Email = "examiner@example.com", Status = status,
            Profile = role is null ? null! : [new ProfileEntity { Role = role }]
        };
        _userGateway.FindById(22).Returns(account);
        _userGateway.FindByEmail("examiner@example.com").Returns(account);
        var dto = new ScheduleTccDTO(new DateOnly(2026, 11, 18), new TimeOnly(14, 0), "Sala 2", 1,
            [new BankingMemberDto("Examinador", "examiner@example.com", "Examinador Interno", lookupByEmail ? null : 22)]);

        var result = await _useCase.Execute(dto, 1);

        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Empty(tcc.BankingMembers);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_RejectsNewScheduleOutsideThePresentationStep()
    {
        var tcc = AdvisorTcc(new TccEntity { Id = 1, Step = "PROPOSAL" });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var result = await _useCase.Execute(new ScheduleTccDTO(new DateOnly(2026, 11, 18), new TimeOnly(14, 0), "Sala 2", 1), 1);

        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Null(tcc.TccSchedule);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Theory]
    [InlineData("pdf")]
    [InlineData("empty")]
    [InlineData("missing")]
    public async Task Execute_ReplacesAnInternalExaminerWithAnExternalInvite(string storageResult)
    {
        var bankingProfile = new ProfileEntity { Id = 9, Role = "BANKING" };
        var advisor = new UserEntity { Id = 1, Name = "Orientador", Email = "advisor@example.com", CampiCourseId = 3 };
        var tcc = AdvisorTcc(new TccEntity
        {
            Id = 1,
            Step = "PRESENTATION_AND_EVALUATION",
            TccFile = "filled/trabalho.pdf",
            TccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(7), Location = "Sala 2" }
        });
        tcc.UserTccs.Single().User = advisor;
        var examiner = new TccBankingMemberEntity("Examinador", "examiner@example.com", "Examinador Interno", "old-token", 1)
        {
            UserId = 22,
            TokenExpiryDate = DateTime.UtcNow.AddDays(1)
        };
        tcc.BankingMembers.Add(examiner);
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(bankingProfile);
        var bytes = new byte[] { 1, 2, 3 };
        _storageGateway.Download(tcc.TccFile, false).Returns(storageResult switch
        {
            "pdf" => Task.FromResult(bytes),
            "empty" => Task.FromResult(Array.Empty<byte>()),
            _ => Task.FromException<byte[]>(new IOException("storage indisponivel"))
        });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
        var dto = new ScheduleTccDTO(new DateOnly(2026, 11, 18), null, null, 1,
            [new BankingMemberDto("Examinador externo", examiner.Email, "Examinador Externo")]);
        var existingDate = tcc.TccSchedule.ScheduledDate;

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingDate, tcc.TccSchedule.ScheduledDate);
        Assert.Equal("Sala 2", tcc.TccSchedule.Location);
        Assert.Null(examiner.UserId);
        Assert.Null(examiner.TokenExpiryDate);
        Assert.NotEqual("old-token", examiner.AccessToken);
        Assert.Contains(tcc.BankingMembers, member => member.UserId == advisor.Id);
        await _tccGateway.Received(1).Update(tcc);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.TypeTemplate == "BANKING-INVITE" && email.Recipient == examiner.Email &&
            (storageResult == "pdf"
                ? email.Attachments != null && email.Attachments.Count == 1 && email.Attachments[0].FileName == "trabalho.pdf"
                : email.Attachments == null)));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.TypeTemplate == "SCHEDULE-TCC-ADVISOR" && email.Attachments == null));
    }

    private static TccEntity AdvisorTcc(TccEntity tcc)
    {
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, Profile = new ProfileEntity { Role = "ADVISOR" } });
        return tcc;
    }
}
