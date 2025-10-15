using FluentValidation;
using gestaotcc.Domain.Dtos.User;

namespace gestaotcc.WebApi.Validators.User;

public class AutoRegisterValidator : AbstractValidator<AutoRegisterDTO>
{
    public AutoRegisterValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres");

        RuleFor(x => x.Email)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.")
            .Must(email => email?.Trim().ToLower().EndsWith(".ifpe.edu.br") == true).WithMessage("O e-mail deve pertencer ao domínio ifpe.edu.br.");

        RuleFor(x => x.CPF)
            .NotEmpty().WithMessage("O campo CPF é obrigatório.")
            .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$").WithMessage("O CPF deve estar no formato XXX.XXX.XXX-XX.")
            .Must(cpf => cpf.Length == 14).WithMessage("O CPF deve ter 14 caracteres, incluindo os pontos e o traço.");
    }
}