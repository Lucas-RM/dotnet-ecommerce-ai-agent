using ECommerce.Application.DTOs;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200)
            .Must(s => s.Trim().Length > 0).WithMessage("Nome é obrigatório.");

        RuleFor(x => x.Email)
            .NotEmpty().MaximumLength(256)
            .EmailAddress()
            .Must(e => e.Trim().Length > 0);

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter ao menos um dígito.")
            .Matches(@"[\W_]").WithMessage("Senha deve conter ao menos um caractere especial.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Senha e confirmação devem ser iguais.");
    }
}

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
