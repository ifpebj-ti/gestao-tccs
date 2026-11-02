using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class EditScheduleTccValidator : AbstractValidator<ScheduleTccDTO>
{
    public EditScheduleTccValidator()
    {
        RuleFor(x => x.ScheduleDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("A data do agendamento deve ser futura.");
        RuleFor(x => x.IdTcc)
            .GreaterThan(0).WithMessage("O ID do TCC deve ser maior que zero.");

        RuleForEach(x => x.BankingMembers)
            .ChildRules(member =>
            {
                member.RuleFor(x => x.UserId)
                    .GreaterThan(0).When(x => x.UserId.HasValue)
                    .WithMessage("O ID do examinador interno deve ser maior que zero.");
                member.RuleFor(x => x.Name)
                    .NotEmpty().When(x => !x.UserId.HasValue)
                    .WithMessage("Nome do membro da banca e obrigatorio.");
                member.RuleFor(x => x.Email)
                    .NotEmpty().EmailAddress().When(x => !x.UserId.HasValue)
                    .WithMessage("Email do membro da banca e invalido.");
                member.RuleFor(x => x.Role)
                    .NotEmpty().When(x => !x.UserId.HasValue)
                    .WithMessage("Papel do membro da banca e obrigatorio.");
            });

        RuleFor(x => x.BankingMembers)
            .Must(members => members is null || members.Where(x => x.UserId.HasValue)
                .Select(x => x.UserId).Distinct().Count() == members.Count(x => x.UserId.HasValue))
            .WithMessage("O mesmo examinador interno não pode ser adicionado mais de uma vez.");

        RuleFor(x => x.BankingMembers)
            .Must(members => members is null || members.Where(x => !x.UserId.HasValue)
                .Select(x => x.Email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == members.Count(x => !x.UserId.HasValue))
            .WithMessage("O mesmo examinador externo não pode ser adicionado mais de uma vez.");
    }
}
