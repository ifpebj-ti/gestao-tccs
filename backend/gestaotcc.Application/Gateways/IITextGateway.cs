using System.IO;
using System.Threading.Tasks;

namespace gestaotcc.Application.Gateways;

public interface IITextGateway
{
    Task<byte[]> ConvertHtmlToPdf(string htmlContent);
}