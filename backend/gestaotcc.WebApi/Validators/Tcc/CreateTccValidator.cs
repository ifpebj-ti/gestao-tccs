using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc
{
    public class CreateTccValidator : AbstractValidator<CreateTccDTO>
    {
        public CreateTccValidator()
        {
            RuleFor(x => x.StudentEmails)
                .NotNull().WithMessage("A lista de e-mails dos estudantes é obrigatória.")
                .NotEmpty().WithMessage("A lista de e-mails dos estudantes não pode estar vazia.");

            RuleForEach(x => x.StudentEmails)
                .NotEmpty().WithMessage("O e-mail do estudante não pode estar vazio.")
                .EmailAddress().WithMessage("O e-mail '{PropertyValue}' não é válido.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("O título é obrigatório.");

            RuleFor(x => x.Summary)
                .MaximumLength(1000).WithMessage("O resumo deve ter no máximo 1000 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Summary));

            RuleFor(x => x.AdvisorId)
                .GreaterThan(0).WithMessage("O ID do orientador deve ser maior que zero.");
        }
    }
}