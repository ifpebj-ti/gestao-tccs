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
    }
}
