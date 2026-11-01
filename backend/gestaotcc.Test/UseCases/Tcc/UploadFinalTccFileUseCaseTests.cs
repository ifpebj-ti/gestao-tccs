using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class UploadFinalTccFileUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly UploadFinalTccFileUseCase _useCase;

    public UploadFinalTccFileUseCaseTests()
    {
        _useCase = new UploadFinalTccFileUseCase(_tccGateway, _storageGateway, _userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenParametersAreInvalid()
    {
        var result = await _useCase.Execute(0, 1, new byte[] { 1 }, "final.pdf", "application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);

        var resultEmptyFile = await _useCase.Execute(1, 1, Array.Empty<byte>(), "final.pdf", "application/pdf");
        Assert.False(resultEmptyFile.IsSuccess);
        Assert.Equal(400, resultEmptyFile.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenUserNotFoundOrInactive()
    {
        _userGateway.FindById(1).Returns((UserEntity?)null);

        var result = await _useCase.Execute(1, 1, new byte[] { 1 }, "final.pdf", "application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccNotFound()
    {
        var user = new UserEntity { Id = 1, Status = "ACTIVE", Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        _userGateway.FindById(1).Returns(user);
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 1, new byte[] { 1 }, "final.pdf", "application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotLinkedNorManagement()
    {
        var user = new UserEntity { Id = 99, Status = "ACTIVE", Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        _userGateway.FindById(99).Returns(user);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 2, Profile = new ProfileEntity { Role = "STUDENT" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 99, new byte[] { 1 }, "final.pdf", "application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenTccIsNotInFinalStage()
    {
        var user = new UserEntity { Id = 1, Status = "ACTIVE", Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 1, Profile = new ProfileEntity { Role = "STUDENT" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 1, new byte[] { 1 }, "final.pdf", "application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Contains("etapa final", result.ErrorDetails?.Detail);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenFileIsNotPdf()
    {
        var user = new UserEntity { Id = 1, Status = "ACTIVE", Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            FinalSubmissionDeadline = DateTime.UtcNow.AddDays(25),
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 1, Profile = new ProfileEntity { Role = "STUDENT" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 1, new byte[] { 1 }, "arquivo.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Contains("PDF", result.ErrorDetails?.Detail);
    }

    [Fact]
    public async Task Execute_ShouldUploadSuccessfully_WhenDataIsValid()
    {
        var user = new UserEntity { Id = 1, Status = "ACTIVE", Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            FinalSubmissionDeadline = DateTime.UtcNow.AddDays(30),
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 1, Profile = new ProfileEntity { Role = "STUDENT" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var fileBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var result = await _useCase.Execute(1, 1, fileBytes, "meu_tcc_final.pdf", "application/pdf");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.StartsWith("final/tcc_1_", result.Data);
        Assert.Equal(result.Data, tcc.FinalTccFile);

        await _storageGateway.Received(1).Send(Arg.Is<string>(s => s.StartsWith("final/tcc_1_")), fileBytes, "application/pdf", true);
        await _tccGateway.Received(1).Update(tcc);
    }
}
