using FluentValidation;
using gestaotcc.Domain.Dtos.AccessCode;

namespace gestaotcc.WebApi.Validators.AccessCode;

public class VerifyAccessCodeValidator : AbstractValidator<VerifyAccessCodeDTO>
{
    public VerifyAccessCodeValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.");

        RuleFor(x => x.AccessCode)
            .NotNull().WithMessage("O campo código de acesso não pode ser nulo.")
            .NotEmpty().WithMessage("O código de acesso é obrigatório");
    }
}
