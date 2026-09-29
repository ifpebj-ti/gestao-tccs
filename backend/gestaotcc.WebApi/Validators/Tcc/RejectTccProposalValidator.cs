using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class RejectTccProposalValidator : AbstractValidator<RejectTccProposalDTO>
{
    public RejectTccProposalValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo da recusa é obrigatório.")
            .MaximumLength(1000).WithMessage("O motivo da recusa deve ter no máximo 1000 caracteres.");
    }
}
