namespace gestaotcc.Application.Gateways;

public interface IMinioGateway
{
    Task Send(string FileName, string filePath);
}