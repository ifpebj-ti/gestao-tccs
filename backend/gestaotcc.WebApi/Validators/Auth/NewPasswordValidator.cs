using FluentValidation;
using gestaotcc.Domain.Dtos.Auth;

namespace gestaotcc.WebApi.Validators.Auth;

public class NewPasswordValidator : AbstractValidator<NewPasswordDTO>
{
    public NewPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.");
        RuleFor(x => x.Password)
            .NotNull().WithMessage("O campo Password não pode ser nulo.")
            .NotEmpty().WithMessage("A senha é obrigatória.");
        RuleFor(x => x.InviteCode)
            .NotNull().WithMessage("O campo InviteCode não pode ser nulo.")
            .NotEmpty().WithMessage("O campo InviteCode é obrigatório.");
    }
}
