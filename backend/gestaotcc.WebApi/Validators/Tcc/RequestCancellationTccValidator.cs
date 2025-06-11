using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class RequestCancellationTccValidator : AbstractValidator<RequestCancellationTccDTO>
{
    public RequestCancellationTccValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo para o cancelamento é obrigatório.")
            .MaximumLength(500).WithMessage("O motivo deve ter no máximo 500 caracteres.");
        RuleFor(x => x.IdTcc)
            .GreaterThan(0).WithMessage("O ID do TCC deve ser maior que zero.");
    }
}
