using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class UpdateTccValidator : AbstractValidator<UpdateTccDTO>
{
    public UpdateTccValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MinimumLength(3).WithMessage("O título deve ter pelo menos 3 caracteres.")
            .MaximumLength(200).WithMessage("O título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("O resumo é obrigatório.")
            .MinimumLength(10).WithMessage("O resumo deve ter pelo menos 10 caracteres.")
            .MaximumLength(2000).WithMessage("O resumo deve ter no máximo 2000 caracteres.");
    }
}