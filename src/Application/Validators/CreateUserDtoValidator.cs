using FIAP.CloudGames.Usuario.API.Application.DTOs;
using FluentValidation;

namespace FIAP.CloudGames.Usuario.API.Application.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Must(ContainLetter).WithMessage("A senha deve conter pelo menos uma letra.")
            .Must(ContainNumber).WithMessage("A senha deve conter pelo menos um número.")
            .Must(ContainSpecial).WithMessage("A senha deve conter pelo menos um caractere especial.");
    }

    private bool ContainLetter(string password) => password.Any(char.IsLetter);
    private bool ContainNumber(string password) => password.Any(char.IsDigit);
    private bool ContainSpecial(string password) => password.Any(ch => !char.IsLetterOrDigit(ch));
}
