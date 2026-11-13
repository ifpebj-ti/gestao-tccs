using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class ConcludePresentationUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ConcludePresentationUseCase> _logger = Substitute.For<IAppLoggerGateway<ConcludePresentationUseCase>>();
    private readonly ConcludePresentationUseCase _useCase;

    public ConcludePresentationUseCaseTests()
    {
        _useCase = new ConcludePresentationUseCase(_tccGateway, _profileGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccScheduling(1).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotAdvisor()
    {
        var tcc = new TccEntity { Id = 1, Status = StatusTccType.IN_PROGRESS.ToString() };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = 99,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10); // currentUserId = 10, mas orientador é 99

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Somente o orientador deste TCC pode gerenciar a banca.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenTccIsAlreadyCompleted()
    {
        var tcc = new TccEntity { Id = 1, Status = StatusTccType.COMPLETED.ToString() };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = 10,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("A apresentação deste TCC já foi concluída.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldConcludePresentation_WhenValid()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Sistema de TCC",
            Status = StatusTccType.IN_PROGRESS.ToString(),
            TccSchedule = new TccScheduleEntity(1, DateTime.UtcNow.AddDays(1), "Sala 101", 1)
        };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = advisorUser.Id,
            User = advisorUser,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });

        // Membro externo
        var externalMember = new TccBankingMemberEntity("Examinador Ext", "ext@teste.com", "Examinador Externo", "token-ext", 1);
        tcc.BankingMembers.Add(externalMember);

        // Membro interno sem nota
        var internalUser = new UserEntity { Id = 20, Name = "Examinador Int", Email = "int@teste.com" };
        var internalMember = new TccBankingMemberEntity(internalUser.Name, internalUser.Email, "Examinador Interno", "", 1)
        {
            UserId = internalUser.Id
        };
        tcc.BankingMembers.Add(internalMember);

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 5, Role = "BANKING" });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(gestaotcc.Domain.Errors.ResultPattern<bool>.SuccessResult(true));

        var result = await _useCase.Execute(1, 10);

        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Update(tcc);
        // Garante que o orientador foi adicionado à banca
        Assert.Contains(tcc.BankingMembers, m => m.Email == advisorUser.Email);
        // Garante que o token do membro externo foi renovado
        Assert.False(string.IsNullOrWhiteSpace(externalMember.AccessToken));
        Assert.True(externalMember.TokenExpiryDate > DateTime.UtcNow);
        // Garante que tentou enviar e-mail para o examinador externo
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "ext@teste.com"));
    }

    [Fact]
    public async Task Execute_ShouldReturnInternalServerError_WhenDatabaseFails()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Sistema de TCC",
            Status = StatusTccType.IN_PROGRESS.ToString()
        };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = advisorUser.Id,
            User = advisorUser,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _tccGateway.Update(Arg.Any<TccEntity>()).Throws(new Exception("Falha de conexão"));

        var result = await _useCase.Execute(1, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao concluir a apresentação do TCC.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldUpdateExistingAdvisorMember_WhenAdvisorAlreadyInBankingMembers()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Sistema de TCC",
            Status = StatusTccType.IN_PROGRESS.ToString()
        };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = advisorUser.Id,
            User = advisorUser,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });

        // Orientador já está na lista de BankingMembers
        var existingAdvisorMember = new TccBankingMemberEntity(advisorUser.Name, advisorUser.Email, "Orientador", "token-adv", 1);
        tcc.BankingMembers.Add(existingAdvisorMember);

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 5, Role = "BANKING" });

        var result = await _useCase.Execute(1, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(advisorUser.Id, existingAdvisorMember.UserId);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldContinueGracefully_WhenEmailGatewayFailsOrThrows()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "orientador@teste.com" };
        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Sistema de TCC",
            Status = StatusTccType.IN_PROGRESS.ToString()
        };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = advisorUser.Id,
            User = advisorUser,
            Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
        });

        // Membro interno sem nota
        var internalUser = new UserEntity { Id = 20, Name = "Examinador Int", Email = "int@teste.com" };
        var internalMember = new TccBankingMemberEntity(internalUser.Name, internalUser.Email, "Examinador Interno", "", 1)
        {
            UserId = internalUser.Id
        };
        tcc.BankingMembers.Add(internalMember);

        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _profileGateway.FindByRole("BANKING").Returns(new ProfileEntity { Id = 5, Role = "BANKING" });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Throws(new Exception("Falha de conexão SMTP"));

        var result = await _useCase.Execute(1, 10);

        // Deve capturar a exceção de e-mail e ainda assim concluir a apresentação com sucesso
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Update(tcc);
    }
}
