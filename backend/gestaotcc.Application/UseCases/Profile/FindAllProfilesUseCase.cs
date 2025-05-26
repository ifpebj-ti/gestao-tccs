using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Profile;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Profile;
public class FindAllProfilesUseCase(IProfileGateway profileGateway)
{
    public async Task<ResultPattern<List<FindAllProfilesDTO>>> Execute()
    {
        var profiles = await profileGateway.FindAll();
        return ResultPattern<List<FindAllProfilesDTO>>.SuccessResult(profiles.Select(ProfileFactory.CreateFindAllProfilesDTO).ToList());
    }
}
