using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ResendInvitationTccEmailUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();

    private readonly ResendInvitationTccEmailUseCase _useCase;

    public ResendInvitationTccEmailUseCaseTests()
    {
        _useCase = new ResendInvitationTccEmailUseCase(_userGateway, _tccGateway, _emailGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenNoInvitesExist()
    {
        // Arrange
        _tccGateway.FindAllInviteTcc().Returns(new List<TccInviteEntity>());

        // Act
        var result = await _useCase.Execute();

        // Assert
        Assert.True(result.IsSuccess);
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_ShouldSendEmailsToAllInvites_WhenInvitesExist()
    {
        // Arrange
        var invite1 = new TccInviteEntity { Id = 1, Email = "aluno1@email.com", Code = "code1" };
        var invite2 = new TccInviteEntity { Id = 2, Email = "aluno2@email.com", Code = "code2" };

        _tccGateway.FindAllInviteTcc().Returns(new List<TccInviteEntity> { invite1, invite2 });

        _emailGateway.Send(Arg.Any<SendEmailDTO>())
            .Returns(ResultPattern<bool>.SuccessResult());

        // Act
        var result = await _useCase.Execute();

        // Assert
        Assert.True(result.IsSuccess);

        await _emailGateway.Received(2).Send(Arg.Any<SendEmailDTO>());

        // Valida se os e-mails foram enviados corretamente para os destinatários esperados
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(dto => dto.Recipient == "aluno1@email.com"));
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(dto => dto.Recipient == "aluno2@email.com"));
    }
}