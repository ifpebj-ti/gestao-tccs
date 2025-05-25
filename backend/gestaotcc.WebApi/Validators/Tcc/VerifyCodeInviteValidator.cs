using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class VerifyCodeInviteValidator : AbstractValidator<VerifyCodeInviteTccDTO>
{
    public VerifyCodeInviteValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.")
            .Must(email => email?.Trim().ToLower().EndsWith(".ifpe.edu.br") == true).WithMessage("O e-mail deve pertencer ao domínio ifpe.edu.br.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("O código de convite é obrigatório.")
            .Length(6).WithMessage("O código de convite deve ter 6 caracteres.");
    }
}