using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class EditScheduleTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly IAppLoggerGateway<EditScheduleTccUseCase> _logger = Substitute.For<IAppLoggerGateway<EditScheduleTccUseCase>>();
    private readonly EditScheduleTccUseCase _useCase;

    public EditScheduleTccUseCaseTests()
    {
        _useCase = new EditScheduleTccUseCase(
            _tccGateway,
            _logger,
            _userGateway,
            _profileGateway,
            _emailGateway,
            _storageGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccNotFound()
    {
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(null));

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotAdvisor()
    {
        var tcc = new TccEntity { Id = 1, TccSchedule = new TccScheduleEntity() };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 99, Profile = new ProfileEntity { Role = "ADVISOR" } });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(dto, 10); // user 10 não é orientador

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Somente o orientador deste TCC pode gerenciar a banca.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccScheduleIsNull()
    {
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = null });
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("TCC ainda não possui agendamento de defesa", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldUpdateSchedule_WhenDataIsValid()
    {
        var originalDate = DateTime.UtcNow.AddDays(1);
        var tccSchedule = new TccScheduleEntity { ScheduledDate = originalDate, Location = "Old Location" };
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = tccSchedule });

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        var newDate = new DateOnly(originalDate.Year, originalDate.Month, originalDate.Day);
        var newTime = new TimeOnly(originalDate.Hour, originalDate.Minute);
        var newLocation = "New Location";

        var dto = new ScheduleTccDTO(newDate, newTime, newLocation, 1);

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        Assert.Equal(DateTimeKind.Utc, tcc.TccSchedule.ScheduledDate.Kind);
        Assert.Equal(newLocation, tcc.TccSchedule.Location);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenSamePersonAddedTwice()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow, Location = "Sala" };
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = tccSchedule });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var duplicateMembers = new List<BankingMemberDto>
        {
            new("Prof A", "profa@teste.com", "Examinador Externo"),
            new("Prof A", "profa@teste.com", "Examinador Externo")
        };
        var dto = new ScheduleTccDTO(null, null, null, 1, duplicateMembers);

        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("A mesma pessoa não pode ser adicionada mais de uma vez à banca.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenSelectedInternalUserIsNotActiveAdvisor()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow, Location = "Sala" };
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = tccSchedule });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        _userGateway.FindById(20).Returns(new UserEntity
        {
            Id = 20,
            Status = "INACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "STUDENT" } }
        });

        var members = new List<BankingMemberDto>
        {
            new("Prof Inativo", "inativo@teste.com", "Examinador Interno", 20)
        };
        var dto = new ScheduleTccDTO(null, null, null, 1, members);

        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("O usuário selecionado não é um orientador ativo.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldUpdateMembersAndSyncBanking_WhenValidMembersProvided()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(2), Location = "Auditório" };
        var advisorUser = new UserEntity { Id = 1, Name = "Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity { Id = 1, TccSchedule = tccSchedule };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, User = advisorUser, Profile = new ProfileEntity { Role = "ADVISOR" } });

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 3, Role = "BANKING" });

        var internalUser = new UserEntity
        {
            Id = 20,
            Name = "Prof Interno Ativo",
            Email = "interno@teste.com",
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADVISOR" } }
        };
        _userGateway.FindById(20).Returns(internalUser);
        _userGateway.FindByEmail("externo@teste.com").Returns((UserEntity?)null);

        var members = new List<BankingMemberDto>
        {
            new(internalUser.Name, internalUser.Email, "Examinador Interno", 20),
            new("Prof Externo", "externo@teste.com", "Examinador Externo")
        };
        var dto = new ScheduleTccDTO(null, null, "Auditório 2", 1, members);

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Update(tcc);
        Assert.Contains(tcc.BankingMembers, m => m.Email == "interno@teste.com");
        Assert.Contains(tcc.BankingMembers, m => m.Email == "externo@teste.com");
        Assert.Contains(tcc.BankingMembers, m => m.Email == advisorUser.Email);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow, Location = "Location" };
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = tccSchedule });

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Throws(new Exception("DB error"));

        var dto = new ScheduleTccDTO(null, null, "New Location", 1);

        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao editar agendamento de defesa do TCC", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenInternalMemberEmailBelongsToInactiveUser()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow, Location = "Sala" };
        var tcc = AdvisorTcc(new TccEntity { TccSchedule = tccSchedule });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        _userGateway.FindByEmail("inativo@teste.com").Returns(new UserEntity
        {
            Id = 40,
            Email = "inativo@teste.com",
            Status = "INACTIVE"
        });

        var members = new List<BankingMemberDto>
        {
            new("Prof Inativo", "inativo@teste.com", "Examinador Interno")
        };
        var dto = new ScheduleTccDTO(null, null, null, 1, members);

        var result = await _useCase.Execute(dto, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("O e-mail informado pertence a uma conta inativa.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldRetainAndUpdateExistingMembers_AndRemoveOldMembers()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(2), Location = "Sala 101" };
        var advisorUser = new UserEntity { Id = 1, Name = "Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity { Id = 1, TccSchedule = tccSchedule };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, User = advisorUser, Profile = new ProfileEntity { Role = "ADVISOR" } });

        // Membro antigo existente que será retido
        var retainedOldMember = new TccBankingMemberEntity("Antigo Retido", "retido@teste.com", "Examinador Externo", "token-ret", 1);
        // Membro antigo que será removido (não enviado no DTO)
        var removedOldMember = new TccBankingMemberEntity("Antigo Removido", "removido@teste.com", "Examinador Externo", "token-rem", 1);
        tcc.BankingMembers.Add(retainedOldMember);
        tcc.BankingMembers.Add(removedOldMember);

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 3, Role = "BANKING" });
        _userGateway.FindByEmail("retido@teste.com").Returns((UserEntity?)null);

        var members = new List<BankingMemberDto>
        {
            new("Novo Nome Retido", "retido@teste.com", "Examinador Externo")
        };
        var dto = new ScheduleTccDTO(null, null, "Sala 102", 1, members);

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Update(tcc);
        Assert.Contains(tcc.BankingMembers, m => m.Email == "retido@teste.com" && m.Name == "Novo Nome Retido");
        Assert.DoesNotContain(tcc.BankingMembers, m => m.Email == "removido@teste.com");
    }

    [Fact]
    public async Task Execute_ShouldDownloadTccFileAndAttachToEmails_WhenTccFileExists()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(2), Location = "Auditório" };
        var advisorUser = new UserEntity { Id = 1, Name = "Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC com Arquivo",
            TccFile = "filled/tcc_final.pdf",
            TccSchedule = tccSchedule
        };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, User = advisorUser, Profile = new ProfileEntity { Role = "ADVISOR" } });

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 3, Role = "BANKING" });
        _storageGateway.Download("filled/tcc_final.pdf", false).Returns(new byte[] { 1, 2, 3, 4 });
        _userGateway.FindByEmail("ext@teste.com").Returns((UserEntity?)null);

        var members = new List<BankingMemberDto>
        {
            new("Membro Ext", "ext@teste.com", "Examinador Externo")
        };
        var dto = new ScheduleTccDTO(null, null, "Auditório", 1, members);

        var result = await _useCase.Execute(dto, 1);

        Assert.True(result.IsSuccess);
        await _storageGateway.Received().Download("filled/tcc_final.pdf", false);
    }

    [Fact]
    public async Task Execute_ShouldHandleExceptionInDownloadTccFile_Gracefully()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(2), Location = "Auditório" };
        var advisorUser = new UserEntity { Id = 1, Name = "Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC com Arquivo",
            TccFile = "filled/tcc_final.pdf",
            TccSchedule = tccSchedule
        };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, User = advisorUser, Profile = new ProfileEntity { Role = "ADVISOR" } });

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 3, Role = "BANKING" });
        _storageGateway.Download("filled/tcc_final.pdf", false).Throws(new Exception("Storage indisponível"));
        _userGateway.FindByEmail("ext@teste.com").Returns((UserEntity?)null);

        var members = new List<BankingMemberDto>
        {
            new("Membro Ext", "ext@teste.com", "Examinador Externo")
        };
        var dto = new ScheduleTccDTO(null, null, "Auditório", 1, members);

        var result = await _useCase.Execute(dto, 1);

        // Deve capturar o erro de storage no TryLoadTccAttachment e prosseguir normalmente com sucesso
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Update(tcc);
    }

    private static TccEntity AdvisorTcc(TccEntity tcc)
    {
        tcc.UserTccs.Add(new UserTccEntity { UserId = 1, Profile = new ProfileEntity { Role = "ADVISOR" } });
        return tcc;
    }
}
