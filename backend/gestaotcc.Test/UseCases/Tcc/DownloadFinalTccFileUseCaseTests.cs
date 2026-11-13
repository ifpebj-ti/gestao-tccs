using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class DownloadFinalTccFileUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly DownloadFinalTccFileUseCase _useCase;

    public DownloadFinalTccFileUseCaseTests()
    {
        var tccFilesUseCase = new TccFilesUseCase(_tccGateway, _userGateway, _storageGateway);
        _useCase = new DownloadFinalTccFileUseCase(tccFilesUseCase, _storageGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccIsNotAccessible()
    {
        _userGateway.FindById(1).Returns((UserEntity?)null);

        var result = await _useCase.Execute(10, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado ou acesso indisponível.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenFinalFileNotSet()
    {
        var user = new UserEntity
        {
            Id = 1,
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADMIN" } }
        };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity { Id = 10, FinalTccFile = null };
        _tccGateway.FindTccById(10).Returns(tcc);

        var result = await _useCase.Execute(10, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("A versão final do TCC ainda não foi enviada.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenStorageReturnsNullOrEmpty()
    {
        var user = new UserEntity
        {
            Id = 1,
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADMIN" } }
        };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity { Id = 10, FinalTccFile = "tcc-10-final.pdf" };
        _tccGateway.FindTccById(10).Returns(tcc);
        _storageGateway.Download("tcc-10-final.pdf", false).Returns(Array.Empty<byte>());

        var result = await _useCase.Execute(10, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Arquivo da versão final não encontrado no armazenamento.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadGateway_WhenStorageThrowsException()
    {
        var user = new UserEntity
        {
            Id = 1,
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADMIN" } }
        };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity { Id = 10, FinalTccFile = "tcc-10-final.pdf" };
        _tccGateway.FindTccById(10).Returns(tcc);
        _storageGateway.Download("tcc-10-final.pdf", false).Throws(new Exception("S3 timeout"));

        var result = await _useCase.Execute(10, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(502, result.ErrorDetails?.Status);
        Assert.Equal("Não foi possível acessar o arquivo no armazenamento.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFile_WhenFileExistsInStorage()
    {
        var user = new UserEntity
        {
            Id = 1,
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "ADMIN" } }
        };
        _userGateway.FindById(1).Returns(user);

        var tcc = new TccEntity { Id = 10, FinalTccFile = "tcc-10-final.pdf" };
        _tccGateway.FindTccById(10).Returns(tcc);

        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        _storageGateway.Download("tcc-10-final.pdf", false).Returns(expectedBytes);

        var result = await _useCase.Execute(10, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedBytes, result.Data.File);
        Assert.Equal("TCC_10_Versao_Final.pdf", result.Data.FileName);
    }
}

