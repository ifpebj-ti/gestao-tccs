using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ConfirmLibraryReceiptUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IUserGateway _userGateway;
    private readonly IEmailGateway _emailGateway;
    private readonly IAppLoggerGateway<ConfirmLibraryReceiptUseCase> _logger;

    public ConfirmLibraryReceiptUseCase(
        ITccGateway tccGateway,
        IUserGateway userGateway,
        IEmailGateway emailGateway,
        IAppLoggerGateway<ConfirmLibraryReceiptUseCase> logger)
    {
        _tccGateway = tccGateway;
        _userGateway = userGateway;
        _emailGateway = emailGateway;
        _logger = logger;
    }

    public async Task<ResultPattern<bool>> Execute(long tccId, long librarianUserId, ConfirmLibraryReceiptDTO dto)
    {
        if (tccId <= 0 || librarianUserId <= 0)
            return ResultPattern<bool>.FailureResult("Parâmetros inválidos.", 400);

        var user = await _userGateway.FindById(librarianUserId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<bool>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var canConfirm = roles.Any(r => r is "ADMIN" or "LIBRARY");
        if (!canConfirm)
            return ResultPattern<bool>.FailureResult("Apenas o setor da Biblioteca ou Administração pode homologar o TCC no acervo.", 403);

        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<bool>.FailureResult("TCC não encontrado.", 404);

        var isFinalStage = tcc.Step == StepTccType.FINALIZATION_AND_PUBLICATION.ToString() ||
                           tcc.Step == "6" ||
                           tcc.FinalSubmissionDeadline.HasValue;

        if (!isFinalStage)
            return ResultPattern<bool>.FailureResult("O TCC não está na etapa final de publicação.", 400);

        if (!tcc.ForwardedToLibraryAt.HasValue)
            return ResultPattern<bool>.FailureResult("O TCC ainda não foi encaminhado pela Coordenação do Curso para a Biblioteca.", 400);

        if (tcc.LibraryReceivedAt.HasValue)
            return ResultPattern<bool>.FailureResult("Este TCC já foi recebido e homologado pela Biblioteca anteriormente.", 409);

        tcc.LibraryReceivedAt = DateTime.UtcNow;
        tcc.LibraryReceivedByUserId = librarianUserId;
        tcc.LibraryCatalogCode = !string.IsNullOrWhiteSpace(dto?.CatalogCode) ? dto.CatalogCode.Trim() : null;
        tcc.Status = StatusTccType.COMPLETED.ToString();
        tcc.Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString();

        await _tccGateway.Update(tcc);

        // Disparo de notificação assíncrona de TCC concluído para estudantes, orientador e coordenação
        try
        {
            var studentNames = string.Join(", ", tcc.UserTccs
                .Where(u => u.Profile.Role == "STUDENT")
                .Select(u => u.User.Name ?? u.User.Email ?? "Aluno"));

            var advisorName = tcc.UserTccs
                .Where(u => u.Profile.Role == "ADVISOR")
                .Select(u => u.User.Name)
                .FirstOrDefault() ?? "Orientador";

            var recipients = new List<(string Email, string Name)>();

            // Estudantes
            foreach (var studentLink in tcc.UserTccs.Where(u => u.Profile.Role == "STUDENT"))
            {
                if (!string.IsNullOrWhiteSpace(studentLink.User.Email))
                    recipients.Add((studentLink.User.Email, studentLink.User.Name ?? "Estudante"));
            }

            // Orientador
            var advisorUser = tcc.UserTccs
                .Where(u => u.Profile.Role == "ADVISOR")
                .Select(u => u.User)
                .FirstOrDefault();

            if (advisorUser != null && !string.IsNullOrWhiteSpace(advisorUser.Email))
                recipients.Add((advisorUser.Email, advisorUser.Name ?? "Orientador"));

            // Coordenadores do curso
            var studentCourseId = tcc.UserTccs
                .Where(u => u.Profile.Role == "STUDENT")
                .Select(u => u.User.CampiCourseId)
                .FirstOrDefault() ?? 0;

            if (studentCourseId > 0)
            {
                var coordinators = await _userGateway.FindAllByFilter(new UserFilterDTO(null, null, null, "COORDINATOR"), studentCourseId);
                foreach (var coord in coordinators.Where(c => c.Status == "ACTIVE" && !string.IsNullOrWhiteSpace(c.Email)))
                {
                    recipients.Add((coord.Email, coord.Name ?? "Coordenação"));
                }
            }

            foreach (var recipient in recipients.DistinctBy(r => r.Email.ToLowerInvariant()))
            {
                var templateVars = new Dictionary<string, object>
                {
                    { "recipientName", recipient.Name },
                    { "tccTitle", tcc.Title ?? string.Empty },
                    { "studentNames", studentNames },
                    { "advisorName", advisorName },
                    { "catalogCode", tcc.LibraryCatalogCode ?? string.Empty }
                };

                var emailDto = new SendEmailDTO(
                    "",
                    $"TCC Concluído com Sucesso - {tcc.Title}",
                    recipient.Email,
                    "TCC-COMPLETED",
                    templateVars);

                await _emailGateway.Send(emailDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mails de conclusão do TCC {TccId}.", tcc.Id);
        }

        return ResultPattern<bool>.SuccessResult(true);
    }
}
