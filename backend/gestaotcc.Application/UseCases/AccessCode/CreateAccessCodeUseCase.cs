using gestaotcc.Application.Factories;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class CreateAccessCodeUseCase
{
    public ResultPattern<AccessCodeEntity> Execute(string combination)
    {
        var accessCode = AccessCodeFactory.CreateAccessCodeEntity(combination);
        return ResultPattern<AccessCodeEntity>.SuccessResult(accessCode);
    }
}
