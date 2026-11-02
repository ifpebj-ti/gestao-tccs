using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class EvaluateRegisteredUserUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IAppLoggerGateway<EvaluateTccUseCase> _logger = Substitute.For<IAppLoggerGateway<EvaluateTccUseCase>>();
    private readonly EvaluateTccUseCase _useCase;

    public EvaluateRegisteredUserUseCaseTests()
    {
        _useCase = new EvaluateTccUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldRejectUserOutsideTheBank()
    {
        _tccGateway.FindBankingMemberByTccAndUser(10, 20).Returns((TccBankingMemberEntity?)null);

        var result = await _useCase.ExecuteRegisteredUser(10, 20, 8m, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().UpdateBankingMember(Arg.Any<TccBankingMemberEntity>());
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldWaitUntilPresentationIsConcluded()
    {
        var member = CreateInternalMember();
        _tccGateway.FindBankingMemberByTccAndUser(10, 20).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(10, 20, 8m, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().UpdateBankingMember(Arg.Any<TccBankingMemberEntity>());
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldRejectDuplicateEvaluation()
    {
        var member = CreateInternalMember();
        member.TokenExpiryDate = DateTime.UtcNow.AddHours(1);
        member.Grade = 8m;
        _tccGateway.FindBankingMemberByTccAndUser(10, 20).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(10, 20, 9m, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().UpdateBankingMember(Arg.Any<TccBankingMemberEntity>());
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldSaveGradeAfterPresentationIsConcluded()
    {
        var member = CreateInternalMember();
        member.TokenExpiryDate = DateTime.UtcNow.AddHours(1);
        _tccGateway.FindBankingMemberByTccAndUser(10, 20).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(10, 20, 8.5m, "Aprovado", "Boa defesa");

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Data);
        Assert.Equal(8.5m, member.Grade);
        Assert.Equal("Aprovado", member.EvaluationComments);
        Assert.Equal("Boa defesa", member.EvaluationDetails);
        await _tccGateway.Received(1).UpdateBankingMember(member);
    }

    private static TccBankingMemberEntity CreateInternalMember() => new()
    {
        Id = 1,
        TccId = 10,
        UserId = 20,
        Role = "Examinador Interno",
        Email = "examiner@example.com"
    };
}
