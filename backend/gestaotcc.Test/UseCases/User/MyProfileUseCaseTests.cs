using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.User;

public class MyProfileUseCaseTests
{
    private readonly IUserGateway _gateway = Substitute.For<IUserGateway>();

    [Theory]
    [InlineData("ADVISOR", 403)]
    [InlineData("STUDENT", 409)]
    public async Task StudentUpdate_RejectsWrongRoleOrConcurrentAccountChange(string role, int status)
    {
        var user = new UserEntity { Id = 21, Name = "Original", Profile = [new() { Role = role }] };
        _gateway.FindById(21).Returns(user);
        var result = await new MyProfileUseCase(_gateway).UpdateStudent(21,
            new("Nome de teste", "81999991234", "123.456.789-09", "TESTE", "Turma", gestaotcc.Domain.Enums.ShiftType.NIGHT));
        Assert.Equal(status, result.ErrorDetails?.Status);
        Assert.Equal("Original", user.Name);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("00000000000", false)]
    [InlineData("12345678900", false)]
    [InlineData("12345678909", true)]
    [InlineData("123.456.789-09", true)]
    [InlineData("abc12345678909", false)]
    public void CpfChecksFormatAndDigits(string cpf, bool expected) =>
        Assert.Equal(expected, gestaotcc.Application.Helpers.StudentProfileValidation.IsValidCpf(cpf));

    [Fact]
    public async Task UpdatePhone_PreservesIdentityPermissionsAndAcademicData()
    {
        var roles = new List<ProfileEntity> { new() { Role = "STUDENT" } };
        var user = new UserEntity
        {
            Id = 21, Name = "Pessoa de Teste", Email = "teste@discente.ifpe.edu.br",
            Phone = "81988888888", Password = "hash-ficticio", Status = "ACTIVE",
            CPF = "000.000.000-00", Registration = "20260001", SIAPE = "1234567",
            CampiCourseId = 9, Profile = roles
        };
        _gateway.FindById(21).Returns(user);
        _gateway.UpdatePhone(21, "81999991234").Returns(true);

        var result = await new MyProfileUseCase(_gateway).UpdatePhone(21, new("(81) 99999-1234"));

        Assert.True(result.IsSuccess);
        Assert.Equal("81999991234", user.Phone);
        Assert.Equal("Pessoa de Teste", user.Name);
        Assert.Equal("teste@discente.ifpe.edu.br", user.Email);
        Assert.Equal("hash-ficticio", user.Password);
        Assert.Equal("ACTIVE", user.Status);
        Assert.Equal("000.000.000-00", user.CPF);
        Assert.Equal("20260001", user.Registration);
        Assert.Equal("1234567", user.SIAPE);
        Assert.Equal(9, user.CampiCourseId);
        Assert.Same(roles, user.Profile);
        await _gateway.Received(1).UpdatePhone(21, "81999991234");
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidIdentity_DoesNotReadOrUpdateAnyUser(long userId)
    {
        var useCase = new MyProfileUseCase(_gateway);
        Assert.Equal(401, (await useCase.Find(userId)).ErrorDetails?.Status);
        Assert.Equal(401, (await useCase.UpdatePhone(userId, new("81999991234"))).ErrorDetails?.Status);
        await _gateway.DidNotReceiveWithAnyArgs().FindById(default);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
        await _gateway.DidNotReceiveWithAnyArgs().UpdatePhone(default, default!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("819999912345678")]
    [InlineData("telefone81999991234")]
    [InlineData("<script>123</script>")]
    public async Task InvalidPhone_DoesNotChangeData(string phone)
    {
        var result = await new MyProfileUseCase(_gateway).UpdatePhone(21, new(phone));
        Assert.Equal(400, result.ErrorDetails?.Status);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task MissingAccount_ReturnsNotFoundWithoutSaving()
    {
        var useCase = new MyProfileUseCase(_gateway);
        Assert.Equal(404, (await useCase.Find(21)).ErrorDetails?.Status);
        Assert.Equal(404, (await useCase.UpdatePhone(21, new("81999991234"))).ErrorDetails?.Status);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task AccountRemovedDuringUpdate_DoesNotReportSuccessOrChangeLoadedData()
    {
        var user = new UserEntity { Id = 21, Phone = "81988888888" };
        _gateway.FindById(21).Returns(user);
        _gateway.UpdatePhone(21, "81999991234").Returns(false);
        var result = await new MyProfileUseCase(_gateway).UpdatePhone(21, new("81999991234"));
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("81988888888", user.Phone);
    }

    [Fact]
    public async Task Find_ReturnsSafeFieldsWithoutPasswordOrCpf()
    {
        _gateway.FindById(21).Returns(new UserEntity { Id = 21, Name = "Teste", Password = "nao-expor", CPF = "nao-expor" });
        var result = await new MyProfileUseCase(_gateway).Find(21);
        var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("Password", json);
        Assert.DoesNotContain("CPF", json);
        Assert.DoesNotContain("nao-expor", json);
    }
}
