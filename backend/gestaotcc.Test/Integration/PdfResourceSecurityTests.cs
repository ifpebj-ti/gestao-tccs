using System.Text;
using gestaotcc.Infra.Gateways;
using gestaotcc.Infra.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace gestaotcc.Test.Integration;

public class PdfResourceSecurityTests
{
    [Theory]
    [InlineData("http://127.0.0.1/private")]
    [InlineData("http://169.254.169.254/metadata")]
    [InlineData("https://example.com/image.png")]
    [InlineData("file:///etc/passwd")]
    public void ResourceRetrieverNeverReadsUrls(string address)
    {
        var retriever = new OfflinePdfResourceRetriever();
        Assert.Throws<IOException>(() => retriever.GetInputStreamByUrl(new Uri(address)));
        Assert.Throws<IOException>(() => retriever.GetByteArrayByUrl(new Uri(address)));
    }

    [Fact]
    public async Task HtmlConversionDoesNotRequestReferencedNetworkResources()
    {
        var requests = 0;
        var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.MapGet("/{**path}", () => { Interlocked.Increment(ref requests); return Results.Text("blocked", "text/plain"); });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var pdf = await new ITextGateway().ConvertHtmlToPdf(
            $"<html><head><link rel='stylesheet' href='{address}/private.css'></head><body><p>Safe text</p><img src='{address}/private.png'></body></html>");
        Assert.Equal(0, requests);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public async Task EmbeddedImagesRemainAvailableWithoutNetworkAccess()
    {
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/wN8AAAAASUVORK5CYII=";
        var pdf = await new ITextGateway().ConvertHtmlToPdf($"<html><body><p>Embedded image</p><img src='data:image/png;base64,{png}'></body></html>");
        using var reader = new iText.Kernel.Pdf.PdfReader(new MemoryStream(pdf));
        using var document = new iText.Kernel.Pdf.PdfDocument(reader);
        Assert.True(document.GetPage(1).GetResources().GetResource(iText.Kernel.Pdf.PdfName.XObject)?.Size() > 0);
    }
}
