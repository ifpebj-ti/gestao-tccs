using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly FindTccUseCase _useCase;
    private readonly IAppLoggerGateway<FindTccUseCase> _logger = Substitute.For<IAppLoggerGateway<FindTccUseCase>>();

    public FindTccUseCaseTests()
    {
        _useCase = new FindTccUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccInformations(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(null));

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WithFindTccDTO()
    {
        // Arrange
        var studentProfile = new ProfileEntity { Role = RoleType.STUDENT.ToString() };
        var advisorProfile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() };
        var bankingProfile = new ProfileEntity { Role = RoleType.BANKING.ToString() };

        var studentUser = new UserEntity
        {
            Name = "Aluno 1",
            Registration = "123",
            CPF = "11122233344",
            Email = "aluno1@email.com",
            CampiCourse = new CampiCourseEntityBuilder()
                .WithCourse(new CourseEntityBuilder().WithName("Engenharia").Build())
                .Build()
        };

        var advisorUser = new UserEntity
        {
            Name = "Orientador 1",
            Email = "orientador@email.com"
        };

        var bankingInternalMember = new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity(
            "Membro Interno", "user@belojardim.edu.br", "IFPE", "token123", 1
        );

        var bankingExternalMember = new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity(
            "Membro Externo", "external@gmail.com", "UFRPE", "token456", 1
        );

        var tccSchedule = new TccScheduleEntity
        {
            ScheduledDate = new DateTime(2025, 8, 1, 14, 0, 0, DateTimeKind.Utc),
            Location = "Sala 101"
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Título do TCC",
            Summary = "Resumo do TCC",
            TccSchedule = tccSchedule,
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = studentUser, Profile = studentProfile },
                new UserTccEntity { User = advisorUser, Profile = advisorProfile }
            },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                bankingInternalMember,
                bankingExternalMember
            },
            TccInvites = new List<TccInviteEntity>(),
            TccCancellation = null
        };

        _tccGateway.FindTccInformations(Arg.Any<long>()).Returns(Task.FromResult((TccEntity?)tcc));

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var dto = result.Data;

        Assert.Equal("Título do TCC", dto.InfoTcc.Title);
        Assert.Equal("Resumo do TCC", dto.InfoTcc.Summary);
        Assert.Equal(new DateOnly(2025, 8, 1), dto.InfoTcc.PresentationDate);
        Assert.Equal(new TimeOnly(14, 0), dto.InfoTcc.PresentationTime);
        Assert.Equal("Sala 101", dto.InfoTcc.PresentationLocation);

        Assert.Single(dto.InfoStudent);
        Assert.Equal("Aluno 1", dto.InfoStudent[0].Name);
        Assert.Equal("123", dto.InfoStudent[0].Registration);
        Assert.Equal("11122233344", dto.InfoStudent[0].CPF);
        Assert.Equal("Engenharia", dto.InfoStudent[0].Course);
        Assert.Equal("aluno1@email.com", dto.InfoStudent[0].Email);

        Assert.Equal("Orientador 1", dto.InfoAdvisor.Name);
        Assert.Equal("orientador@email.com", dto.InfoAdvisor.Email);

        Assert.Equal(2, dto.InfoBanking.Members.Count);
        Assert.Equal("Membro Interno", dto.InfoBanking.Members[0].Name);
        Assert.Equal("user@belojardim.edu.br", dto.InfoBanking.Members[0].Email);
        Assert.Equal("Membro Externo", dto.InfoBanking.Members[1].Name);
        Assert.Equal("external@gmail.com", dto.InfoBanking.Members[1].Email);

        Assert.False(dto.CancellationRequest);
    }
}