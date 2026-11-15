using System.IO.Compression;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using gestaotcc.Application.Gateways;
using gestaotcc.Infra.Gateways;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.Integration;

public class StorageGatewayContractTests
{
    private readonly IAmazonS3 s3 = Substitute.For<IAmazonS3>();
    private StorageGateway Gateway() => new(s3, new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["STORAGE_SETTINGS:BUCKET_NAME"] = "isolated-test" }).Build(), Substitute.For<IITextGateway>());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UploadPreservesBytesAndMimeAndCreatesOnlyMissingBucket(bool bucketExists)
    {
        s3.ListBucketsAsync(Arg.Any<CancellationToken>()).Returns(new ListBucketsResponse
        { Buckets = bucketExists ? [new S3Bucket { BucketName = "isolated-test" }] : [] });
        byte[]? captured = null; PutObjectRequest? request = null;
        s3.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            request = call.Arg<PutObjectRequest>(); using var memory = new MemoryStream();
            request.InputStream.CopyTo(memory); captured = memory.ToArray();
            return Task.FromResult(new PutObjectResponse());
        });
        var bytes = Encoding.UTF8.GetBytes("synthetic-document");
        await Gateway().Send("signature", bytes, "image/png");
        Assert.Equal(bytes, captured); Assert.Equal("signatures/signature.png", request!.Key);
        Assert.Equal("isolated-test", request.BucketName); Assert.Equal("image/png", request.ContentType);
        Assert.False(request.UseChunkEncoding);
        await s3.Received(bucketExists ? 0 : 1).PutBucketAsync(Arg.Is<PutBucketRequest>(r => r.BucketName == "isolated-test"), Arg.Any<CancellationToken>());
        await Gateway().Send("final/document.pdf", bytes, "application/pdf", true);
        Assert.Equal("final/document.pdf", request.Key);
    }

    [Theory]
    [InlineData("document.html", false, "templates/document.html")]
    [InlineData("document.png", true, "signatures/document.png")]
    [InlineData("filled/document.pdf", false, "filled/document.pdf")]
    [InlineData("final/document.pdf", false, "final/document.pdf")]
    public async Task DownloadsUseTheExpectedNamespace(string key, bool signed, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes("content");
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            new GetObjectResponse { ResponseStream = new MemoryStream(bytes) });
        Assert.Equal(bytes, await Gateway().Download(key, signed));
        await s3.Received(1).GetObjectAsync(Arg.Is<GetObjectRequest>(r => r.Key == expected && r.BucketName == "isolated-test"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("NoSuchKey")]
    [InlineData("404")]
    public async Task MissingObjectsReturnEmptyButStorageFailuresAreNotSwallowed(string error)
    {
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("missing") { ErrorCode = error });
        Assert.Empty(await Gateway().Download("unknown.pdf"));
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("access denied") { ErrorCode = "AccessDenied" });
        await Assert.ThrowsAsync<AmazonS3Exception>(() => Gateway().Download("unknown.pdf"));
    }

    [Fact]
    public async Task Base64PreservesDownloadedBytesAndRemoveTargetsOnlyRequestedObject()
    {
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            new GetObjectResponse { ResponseStream = new MemoryStream([0, 1, 2, 255]) });
        Assert.Equal("AAEC/w==", await Gateway().GetDocumentAsBase64("filled/a.pdf", []));
        Assert.Equal("AAEC/w==", await Gateway().GetDocumentAsBase64("a.png", [], true));
        Assert.Equal("AAEC/w==", await Gateway().GetDocumentAsBase64("a.html", []));
        await Gateway().Remove("final/only-this.pdf");
        await s3.Received(1).DeleteObjectAsync(Arg.Is<DeleteObjectRequest>(r => r.Key == "final/only-this.pdf" && r.BucketName == "isolated-test"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ZipAndListingFollowContinuationTokensWithoutLosingFiles()
    {
        var tokens = new List<string?>();
        s3.ListObjectsV2Async(Arg.Any<ListObjectsV2Request>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<ListObjectsV2Request>(); tokens.Add(request.ContinuationToken);
            var first = request.ContinuationToken == null;
            return new ListObjectsV2Response { IsTruncated = first, NextContinuationToken = first ? "page-2" : null,
                S3Objects = [new S3Object { Key = "signatures/group/" + (first ? "a.txt" : "b.txt"), Size = 3, LastModified = DateTime.UtcNow }] };
        });
        s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            new GetObjectResponse { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("abc")) });
        var zip = await Gateway().DownloadFolderAsZip("group");
        using var archive = new ZipArchive(new MemoryStream(zip));
        Assert.Equal(new[] { "a.txt", "b.txt" }, archive.Entries.Select(e => e.FullName));
        foreach (var entry in archive.Entries)
        { using var reader = new StreamReader(entry.Open()); Assert.Equal("abc", await reader.ReadToEndAsync()); }
        var objects = new List<gestaotcc.Domain.Dtos.Signature.StorageObjectDto>();
        await foreach (var item in await Gateway().ListBuckets("signatures/group/")) objects.Add(item);
        Assert.Equal(2, objects.Count);
        Assert.Equal(new string?[] { null, "page-2", null, "page-2" }, tokens);
    }
}
