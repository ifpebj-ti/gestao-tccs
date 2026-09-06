using System.IO;
using System.Threading.Tasks;
using gestaotcc.Application.Gateways;
using iText.Html2pdf;

namespace gestaotcc.Infra.Gateways;

public class ITextGateway : IITextGateway
{
    public Task<byte[]> ConvertHtmlToPdf(string htmlContent)
    {
        var outputStream = new MemoryStream();
        
        var properties = new ConverterProperties();
        properties.SetCreateAcroForm(true);
        
        properties.SetBaseUri(Directory.GetCurrentDirectory());
        
        HtmlConverter.ConvertToPdf(htmlContent, outputStream, properties);

        return Task.FromResult(outputStream.ToArray());
    }
}