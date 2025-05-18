using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;
public class LoginUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway, ITokenGateway tokenGateway)
{
    public async Task<ResultPattern<TokenDTO>> Execute(string userEmail, string userPassword)
    {
        var user = await userGateway.FindByEmail(userEmail);
        if (user is null || user.Status.ToUpper() == "INACTIVE")
            return ResultPattern<TokenDTO>.FailureResult("Erro ao realizar login. Por favor verifique as informações e tente novamente", 409);

        var result = ValidateTypeLogin(user, userEmail, userPassword);
        if (result.IsFailure)
            return ResultPattern<TokenDTO>.FailureResult(result.Message, 409);

        var accessToken = tokenGateway.CreateAccessToken(user);
        var refreshToken = tokenGateway.CreateRefreshToken(user);
        if (accessToken is null || refreshToken is null)
            return ResultPattern<TokenDTO>.FailureResult("Erro ao realizar login, verifique os dados e tente novamente", 409);

        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(accessToken, refreshToken));
    }

    private ResultPattern<TokenDTO> ValidateTypeLogin(UserEntity user, string userEmail, string userPassword)
    {
        if (user.Status.ToUpper() == "ACTIVE")
        {
            var result = bcryptGateway.VerifyHashPassword(user, userPassword);
            if (!result)
                return ResultPattern<TokenDTO>.FailureResult("Erro ao realizar login, verifique os dados e tente novamente", 409);
        }
        // Já verificamos se o status é INACTIVE ou ACTIVE,
        // sobrando apenas quando ele ainda não fez o primeiro acesso
        else
        {
            if (user.Password != userPassword)
            {
                return ResultPattern<TokenDTO>.FailureResult("Erro ao realizar login, verifique os dados e tente novamente", 409);
            }
        }
        return ResultPattern<TokenDTO>.SuccessResult();
    }
}
