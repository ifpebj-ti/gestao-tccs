using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class LoginGovUsecase(IUserGateway userGateway, IGovGateway govGateway)
{
    public async Task<ResultPattern<string>> Execute(GetAuthorizationDTO data)
    {
        
    }
}