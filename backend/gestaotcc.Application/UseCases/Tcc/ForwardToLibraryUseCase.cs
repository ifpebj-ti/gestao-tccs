using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ForwardToLibraryUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IUserGateway _userGateway;
    private readonly IEmailGateway _emailGateway;
    private readonly IAppLoggerGateway<ForwardToLibraryUseCase> _logger;

    public ForwardToLibraryUseCase(
        ITccGateway tccGateway,
        IUserGateway userGateway,
        IEmailGateway emailGateway,
        IAppLoggerGateway<ForwardToLibraryUseCase> logger)
    {
        _tccGateway = tccGateway;
        _userGateway = userGateway;
        _emailGateway = emailGateway;
        _logger = logger;
    }

    public async Task<ResultPattern<bool>> Execute(long tccId, long coordinatorUserId)
    {
        if (tccId <= 0 || coordinatorUserId <= 0)
            return ResultPattern<bool>.FailureResult("Parâmetros inválidos.", 400);

        var user = await _userGateway.FindById(coordinatorUserId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<bool>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var canForward = roles.Any(r => r is "ADMIN" or "COORDINATOR" or "SUPERVISOR");
        if (!canForward)
            return ResultPattern<bool>.FailureResult("Apenas a Coordenação ou Administração pode encaminhar o TCC para a Biblioteca.", 403);

        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<bool>.FailureResult("TCC não encontrado.", 404);

        if (!roles.Contains("ADMIN") && user.CampiCourseId.HasValue && user.CampiCourseId.Value > 0)
        {
            var studentCourses = tcc.UserTccs
                .Where(link => link.Profile.Role == "STUDENT")
                .Select(link => link.User.CampiCourseId)
                .ToList();

            if (studentCourses.Count > 0 && !studentCourses.Contains(user.CampiCourseId))
                return ResultPattern<bool>.FailureResult("Acesso não autorizado para o curso deste TCC.", 403);
        }

        var isFinalStage = tcc.Step == StepTccType.FINALIZATION_AND_PUBLICATION.ToString() ||
                           tcc.Step == "6" ||
                           tcc.FinalSubmissionDeadline.HasValue;

        if (!isFinalStage)
            return ResultPattern<bool>.FailureResult("O TCC não está na etapa final.", 400);

        if (!tcc.HardCoverReceivedAt.HasValue)
            return ResultPattern<bool>.FailureResult("A entrega da cópia em capa dura deve ser confirmada antes de encaminhar à Biblioteca.", 400);

        if (tcc.ForwardedToLibraryAt.HasValue)
            return ResultPattern<bool>.FailureResult("Este TCC já foi encaminhado para a Biblioteca anteriormente.", 409);

        tcc.ForwardedToLibraryAt = DateTime.UtcNow;
        await _tccGateway.Update(tcc);

        // Disparo de notificação assíncrona para a Biblioteca (sem bloquear transação)
        try
        {
            var studentNames = string.Join(", ", tcc.UserTccs
                .Where(u => u.Profile.Role == "STUDENT")
                .Select(u => u.User.Name ?? u.User.Email ?? "Aluno"));

            var advisorName = tcc.UserTccs
                .Where(u => u.Profile.Role == "ADVISOR")
                .Select(u => u.User.Name)
                .FirstOrDefault() ?? "Orientador";

            var studentUser = tcc.UserTccs
                .Where(u => u.Profile.Role == "STUDENT")
                .Select(u => u.User)
                .FirstOrDefault();

            var courseName = studentUser?.CampiCourse?.Course?.Name ?? "Curso Superior";

            var librarians = await _userGateway.FindAllByFilter(new UserFilterDTO(null, null, null, "LIBRARY"), 0);

            foreach (var lib in librarians.Where(l => l.Status == "ACTIVE" && !string.IsNullOrWhiteSpace(l.Email)))
            {
                var templateVars = new Dictionary<string, object>
                {
                    { "tccTitle", tcc.Title ?? string.Empty },
                    { "studentNames", studentNames },
                    { "advisorName", advisorName },
                    { "courseName", courseName }
                };

                var emailDto = new SendEmailDTO(
                    "",
                    $"Novo TCC para o Acervo - {tcc.Title}",
                    lib.Email,
                    "FORWARD-LIBRARY",
                    templateVars);

                await _emailGateway.Send(emailDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail de notificação para a Biblioteca sobre o TCC {TccId}.", tcc.Id);
        }

        return ResultPattern<bool>.SuccessResult(true);
    }
}
