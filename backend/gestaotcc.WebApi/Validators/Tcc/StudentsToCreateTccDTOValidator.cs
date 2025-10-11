using FluentValidation;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.WebApi.Validators.Tcc;

public class StudentsToCreateTccDTOValidator : AbstractValidator<StudentsToCreateTccDTO>
{
    public StudentsToCreateTccDTOValidator()
    {
        RuleFor(x => x.StudentEmail)
            .NotEmpty().WithMessage("O e-mail do estudante é obrigatório.")
            .EmailAddress().WithMessage("O e-mail '{PropertyValue}' não é um endereço válido.");

        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("O ID do curso do estudante deve ser válido.");
    }
}