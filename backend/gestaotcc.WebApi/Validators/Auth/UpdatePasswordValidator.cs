using FluentValidation;
using gestaotcc.Domain.Dtos.Auth;

namespace gestaotcc.WebApi.Validators.Auth;

public class UpdatePasswordValidator : AbstractValidator<UpdatePasswordDTO>
{
    public UpdatePasswordValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.");

        RuleFor(x => x.UserPassword)
            .NotNull().WithMessage("O campo Password não pode ser nulo.")
            .NotEmpty().WithMessage("A senha é obrigatório");
    }
}
