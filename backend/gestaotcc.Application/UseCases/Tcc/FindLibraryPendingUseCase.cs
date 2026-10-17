using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindLibraryPendingUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IUserGateway _userGateway;

    public FindLibraryPendingUseCase(ITccGateway tccGateway, IUserGateway userGateway)
    {
        _tccGateway = tccGateway;
        _userGateway = userGateway;
    }

    public async Task<ResultPattern<LibraryPendingDTO>> Execute(long librarianUserId)
    {
        if (librarianUserId <= 0)
            return ResultPattern<LibraryPendingDTO>.FailureResult("Identificador de usuário inválido.", 400);

        var user = await _userGateway.FindById(librarianUserId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<LibraryPendingDTO>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var isLibrary = roles.Any(r => r is "LIBRARY" or "ADMIN");
        if (!isLibrary)
            return ResultPattern<LibraryPendingDTO>.FailureResult("Acesso restrito ao setor da Biblioteca.", 403);

        // Busca todos os TCCs em andamento e concluídos do campus/curso do usuário (ou geral se ADMIN)
        var inProgressFilter = new TccFilterDTO(null, StatusTccType.IN_PROGRESS.ToString());
        var allInProgress = await _tccGateway.FindAllTccByFilter(inProgressFilter, user.CampiCourseId ?? 0);

        var completedFilter = new TccFilterDTO(null, StatusTccType.COMPLETED.ToString());
        var allCompleted = await _tccGateway.FindAllTccByFilter(completedFilter, user.CampiCourseId ?? 0);

        // 1) TCCs encaminhados pela coordenação aguardando homologação no acervo
        var pendingCataloging = allInProgress
            .Where(t => t.ForwardedToLibraryAt.HasValue && !t.LibraryReceivedAt.HasValue)
            .OrderBy(t => t.ForwardedToLibraryAt)
            .Select(t => new LibraryPendingTccItemDTO(
                t.Id,
                t.Title ?? "TCC sem título",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.Name ?? u.User.Email ?? "Aluno")
                    .ToList(),
                t.UserTccs
                    .Where(u => u.Profile.Role == "ADVISOR")
                    .Select(u => u.User.Name ?? "Orientador")
                    .FirstOrDefault() ?? "Orientador",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.CampiCourse?.Course?.Name)
                    .FirstOrDefault() ?? "Curso Superior",
                t.ForwardedToLibraryAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                t.FinalTccFile,
                null,
                null
            ))
            .ToList();

        // 2) TCCs já homologados no acervo
        var recentlyCataloged = allCompleted
            .Where(t => t.LibraryReceivedAt.HasValue)
            .OrderByDescending(t => t.LibraryReceivedAt)
            .Take(50)
            .Select(t => new LibraryPendingTccItemDTO(
                t.Id,
                t.Title ?? "TCC sem título",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.Name ?? u.User.Email ?? "Aluno")
                    .ToList(),
                t.UserTccs
                    .Where(u => u.Profile.Role == "ADVISOR")
                    .Select(u => u.User.Name ?? "Orientador")
                    .FirstOrDefault() ?? "Orientador",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.CampiCourse?.Course?.Name)
                    .FirstOrDefault() ?? "Curso Superior",
                t.ForwardedToLibraryAt.HasValue ? t.ForwardedToLibraryAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                t.FinalTccFile,
                t.LibraryCatalogCode,
                t.LibraryReceivedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
            ))
            .ToList();

        var dto = new LibraryPendingDTO(
            pendingCataloging.Count,
            recentlyCataloged.Count,
            pendingCataloging,
            recentlyCataloged
        );

        return ResultPattern<LibraryPendingDTO>.SuccessResult(dto);
    }
}
