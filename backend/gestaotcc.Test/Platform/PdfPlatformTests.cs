using gestaotcc.Infra.Gateways;
using PdfSharp.Pdf.IO;

namespace gestaotcc.Test.Platform;

public class PdfPlatformTests
{
    [Fact]
    public async Task HtmlPdfCanBeReadByPdfSharpOnCurrentPlatform()
    {
        var bytes = await new ITextGateway().ConvertHtmlToPdf(
            "<html><body><p>GradusFlow: teste de documento.</p></body></html>");
        Assert.True(bytes.Length > 100);
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount > 0);
    }
}
