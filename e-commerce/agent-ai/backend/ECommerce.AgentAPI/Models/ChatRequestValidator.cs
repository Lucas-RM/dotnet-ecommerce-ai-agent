using FluentValidation;

namespace ECommerce.AgentAPI.Models;

public sealed class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("sessionId é obrigatório e não pode ser vazio.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("message é obrigatória.")
            .MaximumLength(16_000);
    }
}
