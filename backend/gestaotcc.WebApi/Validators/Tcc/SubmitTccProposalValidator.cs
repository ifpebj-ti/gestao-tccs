using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class SubmitTccProposalValidator : AbstractValidator<SubmitTccProposalDTO>
{
    public SubmitTccProposalValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título do TCC é obrigatório.")
            .MaximumLength(255).WithMessage("O título deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("O resumo da proposta é obrigatório.")
            .MaximumLength(255).WithMessage("O resumo deve ter no máximo 255 caracteres.");

        RuleFor(x => x.AdvisorId)
            .GreaterThan(0).WithMessage("Selecione um orientador válido.");
    }
}
