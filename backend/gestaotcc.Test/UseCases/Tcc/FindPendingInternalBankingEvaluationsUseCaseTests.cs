using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindPendingInternalBankingEvaluationsUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly FindPendingInternalBankingEvaluationsUseCase _useCase;

    public FindPendingInternalBankingEvaluationsUseCaseTests()
    {
        _useCase = new FindPendingInternalBankingEvaluationsUseCase(_tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoPendingEvaluationsExist()
    {
        _tccGateway.FindPendingInternalBankingMembers(1).Returns(new List<TccBankingMemberEntity>());

        var result = await _useCase.Execute(1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Execute_ShouldMapAndReturnPendingEvaluations_WhenFound()
    {
        var studentUser = new UserEntity { Id = 100, Name = "Estudante da Silva" };
        var schedule = new TccScheduleEntity(1, DateTime.UtcNow.AddDays(3), "Auditório B", 1);
        var tcc = new TccEntity
        {
            Id = 5,
            Title = "Projeto de IA para Gestão",
            TccSchedule = schedule
        };
        tcc.UserTccs.Add(new UserTccEntity
        {
            UserId = studentUser.Id,
            User = studentUser,
            Profile = new ProfileEntity { Role = "STUDENT" }
        });

        var member = new TccBankingMemberEntity("Prof Interno", "interno@teste.com", "Examinador Interno", "", 5)
        {
            UserId = 1,
            Tcc = tcc
        };

        _tccGateway.FindPendingInternalBankingMembers(1).Returns(new List<TccBankingMemberEntity> { member });

        var result = await _useCase.Execute(1);

        Assert.NotNull(result);
        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(5, dto.TccId);
        Assert.Equal("Projeto de IA para Gestão", dto.TccTitle);
        Assert.Equal("Auditório B", dto.Location);
        Assert.Contains("Estudante da Silva", dto.StudentNames);
    }
}
