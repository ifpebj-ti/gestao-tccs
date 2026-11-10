using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.Signature;

public class RemoveFilledPdfsUseCaseTests
{
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly IAppLoggerGateway<RemoveFilledPdfsUseCase> _logger = Substitute.For<IAppLoggerGateway<RemoveFilledPdfsUseCase>>();
    private readonly RemoveFilledPdfsUseCase _useCase;

    public RemoveFilledPdfsUseCaseTests()
    {
        _useCase = new RemoveFilledPdfsUseCase(_storageGateway, _logger);
    }

    private static async IAsyncEnumerable<StorageObjectDto> ToAsyncEnumerable(IEnumerable<StorageObjectDto> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Execute_ShouldRemoveOldFiles_AndKeepCurrentOrFutureFiles()
    {
        var yesterday = DateTime.Today.AddDays(-1).ToString("dd-MM-yyyy");
        var today = DateTime.Today.ToString("dd-MM-yyyy");
        var tomorrow = DateTime.Today.AddDays(1).ToString("dd-MM-yyyy");

        var oldFile = new StorageObjectDto($"filled/doc_{yesterday}_1.pdf", 100, null);
        var todayFile = new StorageObjectDto($"filled/doc_{today}_2.pdf", 100, null);
        var futureFile = new StorageObjectDto($"filled/doc_{tomorrow}_3.pdf", 100, null);

        _storageGateway.ListBuckets("filled/")
            .Returns(ToAsyncEnumerable(new[] { oldFile, todayFile, futureFile }));

        await _useCase.Execute();

        await _storageGateway.Received(1).Remove(oldFile.Key);
        await _storageGateway.DidNotReceive().Remove(todayFile.Key);
        await _storageGateway.DidNotReceive().Remove(futureFile.Key);
    }

    [Fact]
    public async Task Execute_ShouldIgnoreFiles_WhenNameHasNoUnderscoreOrInvalidDateFormat()
    {
        var fileNoUnderscore = new StorageObjectDto("filled/invalidfilename.pdf", 100, null);
        var fileBadDate = new StorageObjectDto("filled/doc_not-a-date_99.pdf", 100, null);

        _storageGateway.ListBuckets("filled/")
            .Returns(ToAsyncEnumerable(new[] { fileNoUnderscore, fileBadDate }));

        await _useCase.Execute();

        await _storageGateway.DidNotReceive().Remove(Arg.Any<string>());
    }
}

