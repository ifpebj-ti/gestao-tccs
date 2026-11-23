using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class AuditAccessPolicyTests
{
    [Theory]
    [InlineData("STUDENT", 7L, true, true, false, true)]
    [InlineData("STUDENT", 7L, false, false, false, false)]
    [InlineData("ADVISOR", 7L, true, true, true, false)]
    [InlineData("BANKING", 7L, true, true, false, false)]
    [InlineData("COORDINATOR", 7L, false, true, true, false)]
    [InlineData("COORDINATOR", 8L, false, false, false, false)]
    [InlineData("SUPERVISOR", 7L, false, true, true, false)]
    [InlineData("LIBRARY", 7L, false, true, false, false)]
    [InlineData("ADMIN", null, false, true, true, false)]
    public async Task Tcc_RestrictsEachOperationToTheUsersRoleAndScope(
        string role, long? course, bool linked, bool read, bool manage, bool cancel)
    {
        var users = Substitute.For<IUserGateway>();
        var tccs = Substitute.For<ITccGateway>();
        var requester = Account(10, role, course);
        users.FindById(10).Returns(requester);
        var project = new TccEntity
        {
            Id = 1,
            UserTccs =
            [
                new UserTccEntity { UserId = 11, User = Account(11, "STUDENT", 7), Profile = new ProfileEntity { Role = "STUDENT" } }
            ]
        };
        if (linked)
            project.UserTccs.Add(new UserTccEntity { UserId = 10, User = requester, Profile = requester.Profile.Single() });
        tccs.FindTccById(1).Returns(project);
        var access = new TccAccessUseCase(tccs, users);

        Assert.Equal(read, await access.CanRead(1, 10));
        Assert.Equal(manage, await access.CanManage(1, 10));
        Assert.Equal(cancel, await access.CanRequestCancellation(1, 10));
    }

    [Fact]
    public async Task TccDetails_DoNotReturnStudentDataOutsideTheRequestersScope()
    {
        var users = Substitute.For<IUserGateway>();
        users.FindById(10).Returns(Account(10, "COORDINATOR", 8));
        var tccs = Substitute.For<ITccGateway>();
        tccs.FindTccInformations(1).Returns(new TccEntity
        {
            Id = 1,
            UserTccs = [new UserTccEntity { UserId = 11, User = Account(11, "STUDENT", 7), Profile = new() { Role = "STUDENT" } }]
        });
        var useCase = new FindTccUseCase(tccs, users, Substitute.For<IAppLoggerGateway<FindTccUseCase>>());

        var result = await useCase.Execute(1, 10);

        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData("STUDENT", 7L, false)]
    [InlineData("ADVISOR", 7L, false)]
    [InlineData("COORDINATOR", null, false)]
    [InlineData("COORDINATOR", 7L, true)]
    [InlineData("COORDINATOR", 8L, false)]
    [InlineData("SUPERVISOR", 7L, true)]
    [InlineData("ADMIN", null, true)]
    public async Task UserDetails_RequireAdministrationOrManagementWithinTheSameCourse(string role, long? course, bool mayRead)
    {
        var users = Substitute.For<IUserGateway>();
        var target = Account(11, "STUDENT", 7);
        users.FindById(11).Returns(target);
        users.FindById(10).Returns(Account(10, role, course));
        var useCase = new FindUserByIdUseCase(users, Substitute.For<IAppLoggerGateway<FindUserByIdUseCase>>());

        var result = await useCase.Execute(11, 10);

        Assert.Equal(mayRead, result.IsSuccess);
        if (mayRead) Assert.Same(target, result.Data);
        else
        {
            Assert.Equal(404, result.ErrorDetails?.Status);
            Assert.Null(result.Data);
        }
    }

    private static UserEntity Account(long id, string role, long? course) =>
        new() { Id = id, Status = "ACTIVE", CampiCourseId = course, Profile = [new ProfileEntity { Role = role }] };
}
