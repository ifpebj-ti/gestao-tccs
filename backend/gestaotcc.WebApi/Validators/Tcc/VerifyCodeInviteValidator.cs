using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class VerifyCodeInviteValidator : AbstractValidator<VerifyCodeInviteTccDTO>
{
    public VerifyCodeInviteValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("O e-mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O e-mail informado não é válido.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("O código de convite é obrigatório.")
            .Length(6).WithMessage("O código de convite deve ter 6 caracteres.");
    }
}