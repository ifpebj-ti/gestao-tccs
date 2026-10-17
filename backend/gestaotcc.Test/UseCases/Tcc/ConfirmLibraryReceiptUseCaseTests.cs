using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.Tcc;

public class ConfirmLibraryReceiptUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ConfirmLibraryReceiptUseCase> _logger = Substitute.For<IAppLoggerGateway<ConfirmLibraryReceiptUseCase>>();
    private readonly ConfirmLibraryReceiptUseCase _useCase;

    public ConfirmLibraryReceiptUseCaseTests()
    {
        _useCase = new ConfirmLibraryReceiptUseCase(_tccGateway, _userGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenParametersInvalid()
    {
        var result = await _useCase.Execute(0, 1, new ConfirmLibraryReceiptDTO());
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);

        var result2 = await _useCase.Execute(1, 0, new ConfirmLibraryReceiptDTO());
        Assert.False(result2.IsSuccess);
        Assert.Equal(400, result2.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotLibraryOrAdmin()
    {
        var student = new UserEntity
        {
            Id = 5,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "STUDENT" } }
        };
        _userGateway.FindById(5).Returns(student);

        var result = await _useCase.Execute(1, 5, new ConfirmLibraryReceiptDTO());
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenNotForwardedToLibraryYet()
    {
        var librarian = new UserEntity
        {
            Id = 50,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "LIBRARY" } }
        };
        _userGateway.FindById(50).Returns(librarian);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            ForwardedToLibraryAt = null // ainda não encaminhado
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 50, new ConfirmLibraryReceiptDTO());
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Contains("encaminhado", result.ErrorDetails?.Detail ?? "");
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenAlreadyConfirmedByLibrary()
    {
        var librarian = new UserEntity
        {
            Id = 50,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "LIBRARY" } }
        };
        _userGateway.FindById(50).Returns(librarian);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            ForwardedToLibraryAt = DateTime.UtcNow.AddDays(-2),
            LibraryReceivedAt = DateTime.UtcNow.AddDays(-1) // já homologado
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 50, new ConfirmLibraryReceiptDTO());
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldSucceed_UpdateStatusToCompleted_AndSendEmails()
    {
        var librarian = new UserEntity
        {
            Id = 50,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "LIBRARY" } }
        };
        _userGateway.FindById(50).Returns(librarian);

        var student = new UserEntity
        {
            Id = 20,
            Name = "Maria Aluna",
            Email = "maria@ifpe.edu.br",
            CampiCourseId = 3
        };

        var advisor = new UserEntity
        {
            Id = 30,
            Name = "Prof. Silva",
            Email = "silva@ifpe.edu.br"
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Portal de TCCs",
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            ForwardedToLibraryAt = DateTime.UtcNow.AddDays(-1),
            LibraryReceivedAt = null,
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = student, Profile = new ProfileEntity { Role = "STUDENT" } },
                new UserTccEntity { User = advisor, Profile = new ProfileEntity { Role = "ADVISOR" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var coordinator = new UserEntity
        {
            Id = 10,
            Name = "Coord. Carlos",
            Email = "carlos@ifpe.edu.br",
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>())
            .Returns(new List<UserEntity> { coordinator });

        var dto = new ConfirmLibraryReceiptDTO("TMB-2026-0042");
        var result = await _useCase.Execute(1, 50, dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(tcc.LibraryReceivedAt);
        Assert.Equal(50, tcc.LibraryReceivedByUserId);
        Assert.Equal("TMB-2026-0042", tcc.LibraryCatalogCode);
        Assert.Equal(StatusTccType.COMPLETED.ToString(), tcc.Status);

        await _tccGateway.Received(1).Update(tcc);
        await _emailGateway.Received(3).Send(Arg.Is<SendEmailDTO>(e => e.TypeTemplate == "TCC-COMPLETED"));
    }
}
