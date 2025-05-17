using FluentValidation;
using gestaotcc.Domain.Dtos.User;

namespace gestaotcc.WebApi.Validators.User;

public class CreateUserValidator : AbstractValidator<CreateUserDTO>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres");

        RuleFor(x => x.Email)
            .NotNull().WithMessage("O campo Email não pode ser nulo.")
            .NotEmpty().WithMessage("O campo Email é obrigatório.")
            .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.")
            .Must(email => email?.Trim().ToLower().EndsWith(".ifpe.edu.br") == true).WithMessage("O e-mail deve pertencer ao domínio ifpe.edu.br.");

        RuleFor(x => x.Profile)
            .NotNull().WithMessage("O campo Profile não pode ser nulo.")
            .Must(profiles => profiles.Any()).WithMessage("O campo Profile deve conter ao menos um perfil.")
            .Must(profiles => profiles.All(role =>
                role == "ADMIN" ||
                role == "COORDINATOR" ||
                role == "SUPERVISOR" ||
                role == "ADVISOR" ||
                role == "STUDENT" ||
                role == "BANKING" ||
                role == "LIBRARY"))
            .WithMessage("Todos os perfis devem ser ADMIN, COORDINATOR, SUPERVISOR, ADVISOR, STUDENT, BANKING ou LIBRARY.");
    }
}
