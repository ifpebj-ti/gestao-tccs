using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.Gateways;

public interface IEmailGateway
{
    Task<ResultPattern<bool>> Send(SendEmailDTO emailDto);
}