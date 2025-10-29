namespace gestaotcc.Application.Gateways;

public interface IITextGateway
{
    Task<MemoryStream> FillPdf(Dictionary<string, string> fields, MemoryStream pdfDocument);
    Task<(string hashToSignB64, byte[] preparedPdf)> PrepareEnvelopedSignature(byte[] pdfDocument);
    Task<byte[]> EmbedEnvelopedSignature(byte[] preparedPdf, string base64Signature);
}