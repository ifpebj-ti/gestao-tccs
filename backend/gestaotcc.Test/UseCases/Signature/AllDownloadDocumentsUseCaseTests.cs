using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Tcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class AllDownloadDocumentsUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IMinioGateway _minioGateway = Substitute.For<IMinioGateway>();
    private readonly AllDownloadDocumentsUseCase _useCase;

    public AllDownloadDocumentsUseCaseTests()
    {
        _useCase = new AllDownloadDocumentsUseCase(_tccGateway, _minioGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturn404_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("Erro ao carregar arquivos", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturn404_WhenZipFileIsEmpty()
    {
        // Arrange
        var tcc = new TccEntity { Id = 1, Title = "Meu TCC" };
        _tccGateway.FindTccById(1).Returns(tcc);

        _minioGateway.DownloadFolderAsZip(Arg.Any<string>())
            .Returns(Array.Empty<byte>());

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("Erro ao carregar arquivos", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenZipFileIsValid()
    {
        // Arrange
        var tccTitle = "Meu TCC";
        var tcc = new TccEntity { Id = 1, Title = tccTitle };
        var currentYear = DateTime.Now.Year;
        var expectedFolder = $"{currentYear}/{tccTitle}";
        var zipBytes = Encoding.UTF8.GetBytes("arquivo.zip");

        _tccGateway.FindTccById(1).Returns(tcc);
        _minioGateway.DownloadFolderAsZip(expectedFolder).Returns(zipBytes);

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tccTitle, result.Data!.FolderName);
        Assert.Equal(zipBytes, result.Data.File);
    }
}