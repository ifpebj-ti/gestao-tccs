using gestaotcc.Domain.Dtos.Email;

namespace gestaotcc.Application.Gateways;

public interface IEmailGateway
{
    Task Send(SendEmailDTO emailDto);
}