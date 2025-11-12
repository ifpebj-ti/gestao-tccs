using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Utils;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.UseCases.User;
public class FindAllUserUseCase(IUserGateway userGateway, IAppLoggerGateway<FindAllUserUseCase> logger)
{
    public async Task<ResultPattern<PagedResultResponse<List<FindAllUserDTO>>>> Execute(int pageNumber, int pageSize)
    {
        logger.LogInformation("Iniciando a busca de todos os usuários da página {page}", pageNumber);

        var users = await userGateway.FindAll(pageNumber, pageSize);

        var totalPages = (long)Math.Ceiling((double)users.TotalRecords / pageSize);

        logger.LogInformation("Finalizando a busca dos usuários da página {page}", pageNumber);

        return ResultPattern<PagedResultResponse<List<FindAllUserDTO>>>.SuccessResult(CreatePagedResultResponse(users, pageNumber, pageSize, totalPages));
        
    }

    private PagedResultResponse<List<FindAllUserDTO>> CreatePagedResultResponse(PagedResult<List<UserEntity>> pagedResult, int pageNumber, int pageSize, long totalPages)
    {
        return new PagedResultResponse<List<FindAllUserDTO>>(
                pagedResult.Pages.Select(u => new FindAllUserDTO(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Profile.Select(p => p.Role).FirstOrDefault()!,
                    u.Registration,
                    u.CPF,
                    u.SIAPE,
                    new CampiDetailsForFindAllUserDTO(
                        u.CampiCourse!.CampiId,
                        u.CampiCourse.Campi.Name,
                        new CourseDetailsForFindAllUserDTO(
                            u.CampiCourse.CourseId,
                            u.CampiCourse.Course.Name
                            )
                    ),
                    u.Status
                )).ToList(),
                pagedResult.TotalRecords,
                pageNumber,
                pageSize,
                totalPages
               );
    }
}

