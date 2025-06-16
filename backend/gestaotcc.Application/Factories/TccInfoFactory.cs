using gestaotcc.Domain.Dtos.InfosTcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Application.Factories;
public class TccInfoFactory
{
    public static FindTccDTO CreateFindTccDTO(TccEntity tcc)
    {
        // 1. Estudantes cadastrados
        var studentUserTccs = tcc.UserTccs
            .Where(ut => ut.Profile.Role == RoleType.STUDENT.ToString())
            .ToList();

        var students = studentUserTccs
            .Select(ut => new InfoStudentDTO(
                Name: ut.User.Name,
                Registration: ut.User.Registration,
                CPF: ut.User.CPF,
                Course: ut.User.Course.Name,
                Email: ut.User.Email
            ))
            .ToList();

        // 2. Estudantes ainda não cadastrados (só tem invite)
        var existingStudentEmails = studentUserTccs
            .Select(ut => ut.User.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invitedStudents = tcc.TccInvites
            .Where(inv => inv.IsValidCode && !existingStudentEmails.Contains(inv.Email))
            .Select(inv => new InfoStudentDTO(
                Name: string.Empty,
                Registration: string.Empty,
                CPF: string.Empty,
                Course: string.Empty,
                Email: inv.Email
            ));

        students.AddRange(invitedStudents);

        // 3. Orientador
        var advisor = tcc.UserTccs
            .FirstOrDefault(ut => ut.Profile.Role == RoleType.ADVISOR.ToString());

        // 4. Membros da banca avaliadora
        var bankingMembers = tcc.UserTccs
            .Where(ut => ut.Profile.Role == RoleType.BANKING.ToString())
            .ToList();

        var internalMember = bankingMembers
            .FirstOrDefault(ut => ut.User.Email.Contains("@belojardim", StringComparison.OrdinalIgnoreCase));

        var externalMember = bankingMembers
            .FirstOrDefault(ut => !ut.User.Email.Contains("@belojardim", StringComparison.OrdinalIgnoreCase));

        // 5. Verificar se o TCC foi solicitado para cancelamento
        bool isCancellationRequest = tcc.TccCancellation is not null;

        // 6. Criação do DTO
        return new FindTccDTO(
            InfoTcc: new InfoTccDTO(
                Title: tcc.Title ?? string.Empty,
                Summary: tcc.Summary ?? string.Empty,
                PresentationDate: tcc.TccSchedule != null
                    ? DateOnly.FromDateTime(tcc.TccSchedule.ScheduledDate)
                    : (DateOnly?)null,
                PresentationTime: tcc.TccSchedule != null
                    ? TimeOnly.FromDateTime(tcc.TccSchedule.ScheduledDate)
                    : (TimeOnly?)null,
                PresentationLocation: tcc.TccSchedule?.Location ?? string.Empty
            ),
            InfoStudent: students,
            InfoAdvisor: new InfoAdvisorDTO(
                Name: advisor!.User.Name,
                Email: advisor!.User.Email
            ),
            InfoBanking: new InfoBankingDTO(
                NameInternal: internalMember?.User.Name ?? string.Empty,
                EmailInternal: internalMember?.User.Email ?? string.Empty,
                NameExternal: externalMember?.User.Name ?? string.Empty,
                EmailExternal: externalMember?.User.Email ?? string.Empty
            ),
            CancellationRequest: isCancellationRequest
        );
    }
}
