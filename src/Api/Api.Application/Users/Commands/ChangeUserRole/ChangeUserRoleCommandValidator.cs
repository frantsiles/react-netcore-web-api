using FluentValidation;

namespace Api.Application.Users.Commands.ChangeUserRole;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.NewRoleName)
            .NotEmpty().WithMessage("NewRoleName is required.");
    }
}
