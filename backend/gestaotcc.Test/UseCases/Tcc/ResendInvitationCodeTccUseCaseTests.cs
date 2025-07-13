using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ResendInvitationCodeTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly ResendInvitationCodeTccUseCase _useCase;

    public ResendInvitationCodeTccUseCaseTests()
    {
        _useCase = new ResendInvitationCodeTccUseCase(_emailGateway, _tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturn404_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(Arg.Any<long>()).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute("email@email.com", 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("Erro ao gerar novo código de acesso", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturn404_WhenInviteNotFoundInTcc()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 1,
            TccInvites = new List<TccInviteEntity>() // Lista vazia
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        // Act
        var result = await _useCase.Execute("email@email.com", 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("Erro ao gerar novo código de acesso", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldGenerateNewCode_UpdateInvite_AndSendEmail()
    {
        // Arrange
        var email = "email@email.com";
        var oldCode = "ABC123";

        var invite = new TccInviteEntity
        {
            Email = email,
            Code = oldCode,
            IsValidCode = false
        };

        var tcc = new TccEntity
        {
            Id = 1,
            TccInvites = new List<TccInviteEntity> { invite }
        };

        _tccGateway.FindTccById(1).Returns(tcc);

        // Falso retorno do EmailGateway (não precisa validar sucesso aqui)
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult());

        // Act
        var result = await _useCase.Execute(email, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(oldCode, invite.Code); // Novo código gerado
        Assert.True(invite.IsValidCode);       // Código agora é válido
        await _tccGateway.Received(1).UpdateTccInvite(invite);
        await _emailGateway.Received(1).Send(Arg.Any<SendEmailDTO>());
    }
}