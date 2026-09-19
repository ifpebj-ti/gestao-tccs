using System.IO;
using System.Threading.Tasks;
using gestaotcc.Application.Gateways;
using iText.Html2pdf;

namespace gestaotcc.Infra.Gateways;

public class ITextGateway : IITextGateway
{
    public Task<byte[]> ConvertHtmlToPdf(string htmlContent)
    {
        using var memoryStream = new MemoryStream();
        HtmlConverter.ConvertToPdf(htmlContent, memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }
}