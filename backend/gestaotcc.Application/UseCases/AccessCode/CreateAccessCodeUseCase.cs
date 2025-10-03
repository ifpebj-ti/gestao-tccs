using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class CreateAccessCodeUseCase(IAppLoggerGateway<CreateAccessCodeUseCase> logger)
{
    public virtual ResultPattern<AccessCodeEntity> Execute(string combination)
    {
        logger.LogInformation("Iniciando a criação de AccessCode");
        
        var accessCode = AccessCodeFactory.CreateAccessCodeEntity(combination);
        
        logger.LogInformation("AccessCode criado com sucesso. AccessCode: {AccessCode}", accessCode.Code);
        
        return ResultPattern<AccessCodeEntity>.SuccessResult(accessCode);
    }
}
