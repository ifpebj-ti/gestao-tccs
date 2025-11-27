using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc
{
    public class CreateTccValidator : AbstractValidator<CreateTccDTO>
    {
        public CreateTccValidator()
        {
            RuleFor(x => x.Students)
                .NotEmpty().WithMessage("A lista de estudantes não pode ser vazia.");

            RuleForEach(x => x.Students)
                .SetValidator(new StudentsToCreateTccDTOValidator());

            RuleFor(x => x.AdvisorId)
                .GreaterThan(0).WithMessage("O ID do orientador deve ser maior que zero.");
        }
    }
}