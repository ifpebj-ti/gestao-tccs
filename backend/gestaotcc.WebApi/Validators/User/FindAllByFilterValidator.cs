using FluentValidation;
using gestaotcc.Domain.Dtos.User;

namespace gestaotcc.WebApi.Validators.User;

public class FindAllByFilterValidator : AbstractValidator<UserFilterDTO>
{
    public FindAllByFilterValidator()
    {
        RuleFor(x => x.Profile)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.Email) && string.IsNullOrEmpty(x.Name))
            .WithMessage("Profile é obrigatório quando não houver Email ou Name.");
    }
}