namespace gestaotcc.Application.Gateways;

public interface IPdfSharpGateway
{
    string? GetHashValidatorToPdf(string pdfPath);
}