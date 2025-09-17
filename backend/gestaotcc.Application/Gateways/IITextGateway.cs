namespace gestaotcc.Application.Gateways;

public interface IITextGateway
{
    Task<MemoryStream> FillPdf(Dictionary<string, string> fields, MemoryStream pdfDocument);
}