using FluentValidation;

namespace ECommerce.AgentAPI.Models;

public sealed class ClearSessionRequestValidator : AbstractValidator<ClearSessionRequest>
{
    public ClearSessionRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("sessionId é obrigatório e não pode ser vazio.");
    }
}
