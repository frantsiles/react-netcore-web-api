using FluentValidation;

namespace Api.Application.Assistant;

public record AskRequest(string Question);

public class AskRequestValidator : AbstractValidator<AskRequest>
{
    public AskRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("La pregunta no puede estar vacía.")
            .MaximumLength(1000).WithMessage("La pregunta no puede superar los 1000 caracteres.");
    }
}
