using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindTccWorkflowUseCase(ITccGateway tccGateway, IDocumentTypeGateway documentTypeGateway)
{
    public async Task<ResultPattern<FindTccWorkflowDTO>> Execute(long tccId, long userId)
    {
        var tcc = await tccGateway.FindTccWorkflow(tccId, userId);
        var documentsType = await documentTypeGateway.FindAll();
        return ResultPattern<FindTccWorkflowDTO>.SuccessResult(TccFactory.CreateFindTccWorkflowDTO(tcc!, documentsType));
    }
}