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
    }
}
