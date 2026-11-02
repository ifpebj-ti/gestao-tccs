using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class LinkBankingUserUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly LinkBankingUserUseCase _useCase;
    private readonly IAppLoggerGateway<LinkBankingUserUseCase> _logger = Substitute.For<IAppLoggerGateway<LinkBankingUserUseCase>>();

    public LinkBankingUserUseCaseTests()
    {
        _useCase = new LinkBankingUserUseCase(_tccGateway, _userGateway, _profileGateway, _emailGateway, _storageGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccNotExists()
    {
        // Arrange
        var dto = new LinkBankingUserDTO(1, 2, 3);
        _tccGateway.FindTccById(dto.idTcc).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(dto, 9);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TCC não encontrado.", result.Message);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenAnyUserIsNull()
    {
        // Arrange
        var dto = new LinkBankingUserDTO(1, 2, 3);
        _tccGateway.FindTccById(dto.idTcc).Returns(AdvisorTcc(new TccEntity()));
        _userGateway.FindById(dto.idInternalBanking).Returns((UserEntity?)null);
        _userGateway.FindById(dto.idExternalBanking).Returns(new UserEntity());

        // Act
        var result = await _useCase.Execute(dto, 9);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Usuário interno ou externo não encontrado.", result.Message);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldRejectExaminer_WhenUserIsNotTccAdvisor()
    {
        var dto = new LinkBankingUserDTO(1, 2, 3);
        var tcc = new TccEntity
        {
            UserTccs =
            {
                new gestaotcc.Domain.Entities.UserTcc.UserTccEntity
                {
                    UserId = 12,
                    Profile = new ProfileEntity { Role = "BANKING" }
                }
            }
        };
        _tccGateway.FindTccById(dto.idTcc).Returns(tcc);

        var result = await _useCase.Execute(dto, 12);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenBankingProfileNotFound()
    {
        // Arrange
        var dto = new LinkBankingUserDTO(1, 2, 3);

        var tcc = AdvisorTcc(new TccEntity { UserTccs = [], TccInvites = [] });
        var user1 = new UserEntity { Id = 1, Email = "int@teste.com", Name = "Interno" };
        var user2 = new UserEntity { Id = 2, Email = "ext@teste.com", Name = "Externo" };

        _tccGateway.FindTccById(dto.idTcc).Returns(tcc);
        _userGateway.FindById(dto.idInternalBanking).Returns(user1);
        _userGateway.FindById(dto.idExternalBanking).Returns(user2);
        _profileGateway.FindByRole("BANKING").Returns((ProfileEntity?)null);
        
        // Act
        var result = await _useCase.Execute(dto, 9);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Erro ao buscar perfil.", result.Message);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldLinkUsers_WhenAllValid()
    {
        // Arrange
        var dto = new LinkBankingUserDTO(1, 2, 3);

        var userInternal = new UserEntity { Id = 1, Email = "internal@example.com", Name = "User Internal" };
        var userExternal = new UserEntity { Id = 2, Email = "external@example.com", Name = "User External" };

        var profileBanking = new ProfileEntity { Role = "BANKING" };

        var tcc = new TccEntity
        {
            Id = dto.idTcc,
            Title = "TCC de Teste",
            UserTccs = [],
            TccInvites = new List<TccInviteEntity>
            {
                new TccInviteEntity { Email = userInternal.Email, IsValidCode = true },
                new TccInviteEntity { Email = userExternal.Email, IsValidCode = true }
            }
        };
        AdvisorTcc(tcc);

        _tccGateway.FindTccById(dto.idTcc).Returns(tcc);
        _userGateway.FindById(dto.idInternalBanking).Returns(userInternal);
        _userGateway.FindById(dto.idExternalBanking).Returns(userExternal);
        _profileGateway.FindByRole("BANKING").Returns(profileBanking);


        // Act
        var result = await _useCase.Execute(dto, 9);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);

        // Verifica se os usuários foram vinculados
        await _tccGateway.Received(1).Update(Arg.Is<TccEntity>(t =>
            t.UserTccs.Any(u => u.User.Id == userInternal.Id) &&
            t.UserTccs.Any(u => u.User.Id == userExternal.Id)
        ));

    }

    private static TccEntity AdvisorTcc(TccEntity tcc)
    {
        tcc.UserTccs.Add(new gestaotcc.Domain.Entities.UserTcc.UserTccEntity
        {
            User = new UserEntity { Id = 9, Email = "advisor@example.com", Name = "Advisor" },
            UserId = 9,
            Profile = new ProfileEntity { Role = "ADVISOR" }
        });
        return tcc;
    }
}
