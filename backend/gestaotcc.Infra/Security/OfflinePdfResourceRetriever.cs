using iText.StyledXmlParser.Resolver.Resource;

namespace gestaotcc.Infra.Security;

// Embedded data-URI images are handled by pdfHTML itself. No URL may trigger network or file I/O.
public sealed class OfflinePdfResourceRetriever : IResourceRetriever
{
    public Stream GetInputStreamByUrl(Uri url) => throw new IOException("Recurso externo não permitido na conversão de PDF.");
    public byte[] GetByteArrayByUrl(Uri url) => throw new IOException("Recurso externo não permitido na conversão de PDF.");
}
