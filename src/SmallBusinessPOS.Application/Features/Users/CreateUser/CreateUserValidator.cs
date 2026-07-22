using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email, clave temporal y rol son obligatorios.")
            .EmailAddress().WithMessage("El email no tiene un formato valido.")
            .MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Email, clave temporal y rol son obligatorios.");
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Email, clave temporal y rol son obligatorios.");
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}
