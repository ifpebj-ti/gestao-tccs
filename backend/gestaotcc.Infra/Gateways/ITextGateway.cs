using System.IO;
using System.Threading.Tasks;
using gestaotcc.Application.Gateways;
using iText.Html2pdf;

namespace gestaotcc.Infra.Gateways;

public class ITextGateway : IITextGateway
{
    public Task<byte[]> ConvertHtmlToPdf(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent) || System.Text.Encoding.UTF8.GetByteCount(htmlContent) > 2 * 1024 * 1024)
            throw new gestaotcc.Domain.Exceptions.DomainException("O documento HTML deve ter conteúdo e até 2 MiB.");
        using var memoryStream = new MemoryStream();
        var properties = new ConverterProperties().SetResourceRetriever(new gestaotcc.Infra.Security.OfflinePdfResourceRetriever());
        HtmlConverter.ConvertToPdf(htmlContent, memoryStream, properties);
        return Task.FromResult(memoryStream.ToArray());
    }
}
