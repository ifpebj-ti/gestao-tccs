using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class RequestAdvisorChangeValidator : AbstractValidator<RequestAdvisorChangeDTO>
{
    public RequestAdvisorChangeValidator()
    {
        RuleFor(x => x.TccId)
            .GreaterThan(0).WithMessage("O ID do TCC deve ser maior que zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A justificativa é obrigatória.")
            .MinimumLength(20).WithMessage("A justificativa deve conter pelo menos 20 caracteres.")
            .MaximumLength(1000).WithMessage("A justificativa deve ter no máximo 1000 caracteres.");

        RuleFor(x => x.RequesterRole)
            .NotEmpty().WithMessage("O papel do solicitante é obrigatório.")
            .Must(role => role.Equals("STUDENT", StringComparison.OrdinalIgnoreCase) || role.Equals("ADVISOR", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O papel do solicitante deve ser STUDENT ou ADVISOR.");
    }
}
