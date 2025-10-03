using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Profile;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Profile;
public class FindAllProfilesUseCase(IProfileGateway profileGateway, IAppLoggerGateway<FindAllProfilesUseCase> logger)
{
    public async Task<ResultPattern<List<FindAllProfilesDTO>>> Execute()
    {
        logger.LogInformation("Iniciando a busca por todos os perfis.");

        var profiles = await profileGateway.FindAll();
        
        logger.LogInformation("Busca no gateway concluída. {ProfileCount} perfis encontrados.", profiles.Count);

        var resultDTO = profiles.Select(ProfileFactory.CreateFindAllProfilesDTO).ToList();

        logger.LogInformation("Mapeamento para DTO concluído. Retornando {ProfileCount} perfis.", resultDTO.Count);

        return ResultPattern<List<FindAllProfilesDTO>>.SuccessResult(resultDTO);
    }
}