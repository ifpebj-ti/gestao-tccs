using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class CreateScheduleTccValidator : AbstractValidator<ScheduleTccDTO>
{
    public CreateScheduleTccValidator()
    {
        RuleFor(x => x.ScheduleDate)
            .NotEmpty().WithMessage("A data do agendamento é obrigatória.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("A data do agendamento deve ser futura.");
        RuleFor(x => x.ScheduleTime)
            .NotEmpty().WithMessage("O horário do agendamento é obrigatório.");
        RuleFor(x => x.ScheduleLocation)
            .NotEmpty().WithMessage("O local do agendamento é obrigatório.");
        RuleFor(x => x.IdTcc)
            .GreaterThan(0).WithMessage("O ID do TCC deve ser maior que zero.");

        RuleForEach(x => x.BankingMembers)
            .ChildRules(members =>
            {
                members.RuleFor(m => m.Name).NotEmpty().WithMessage("Nome do membro da banca é obrigatório.");
                members.RuleFor(m => m.Email).NotEmpty().EmailAddress().WithMessage("Email do membro da banca é inválido.");
                members.RuleFor(m => m.Role).NotEmpty().WithMessage("Papel do membro da banca é obrigatório.");
            });
    }
}
