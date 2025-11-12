using FluentValidation;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Enums;

namespace gestaotcc.WebApi.Validators.User
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserDTO>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("O Id do usuário é obrigatório.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nome é obrigatório")
                .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres");

            RuleFor(x => x.Email)
                .NotNull().WithMessage("O campo Email não pode ser nulo.")
                .NotEmpty().WithMessage("O campo Email é obrigatório.")
                .EmailAddress().WithMessage("O campo Email deve conter um endereço de e-mail válido.")
                .Must(email => email?.Trim().ToLower().EndsWith(".ifpe.edu.br") == true)
                .WithMessage("O e-mail deve pertencer ao domínio ifpe.edu.br.");

            RuleFor(x => x.Registration)
                .NotEmpty().WithMessage("A matrícula é obrigatória.");

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("O campo CPF é obrigatório.")
                .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$").WithMessage("O CPF deve estar no formato XXX.XXX.XXX-XX.")
                .Must(cpf => cpf.Length == 14).WithMessage("O CPF deve ter 14 caracteres, incluindo os pontos e o traço.");

            RuleFor(x => x.Profile)
                .NotNull().WithMessage("O campo Profile não pode ser nulo.")
                .Must(profiles => profiles != null && profiles.Any()).WithMessage("O campo Profile deve conter ao menos um perfil.")
                .Must(profiles => profiles == null || profiles.All(role =>
                    role == "ADMIN" ||
                    role == "COORDINATOR" ||
                    role == "SUPERVISOR" ||
                    role == "ADVISOR" ||
                    role == "STUDENT" ||
                    role == "BANKING" ||
                    role == "LIBRARY"))
                .WithMessage("Todos os perfis devem ser ADMIN, COORDINATOR, SUPERVISOR, ADVISOR, STUDENT, BANKING ou LIBRARY.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("O status é obrigatório.")
                .Must(s => s != null && (s.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) || s.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase)))
                .WithMessage("O status deve ser 'ACTIVE' ou 'INACTIVE'.");

            RuleFor(x => x.CampiId)
                .NotEmpty().WithMessage("O ID do Campus é obrigatório.");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("O ID do Curso é obrigatório.");

            RuleFor(x => x.Siape)
                .NotEmpty().WithMessage("O SIAPE é obrigatório para o perfil SUPERVISOR ou ADVISOR.")
                .When(x => x.Profile != null && x.Profile.Any(p =>
                        p == RoleType.SUPERVISOR.ToString() || // SUPERVISOR
                        p == RoleType.ADVISOR.ToString()));      // ADVISOR
        }
    }
}
