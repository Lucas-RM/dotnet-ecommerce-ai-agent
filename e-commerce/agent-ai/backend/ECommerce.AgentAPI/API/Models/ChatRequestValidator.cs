using FluentValidation;
using System.Text.Json;

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

        RuleFor(x => x.ClientVersion)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.ClientVersion));

        RuleFor(x => x.Locale)
            .MaximumLength(16)
            .When(x => !string.IsNullOrWhiteSpace(x.Locale));

        RuleFor(x => x.Channel)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.Channel));

        RuleFor(x => x.CorrelationId)
            .MaximumLength(128)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.Metadata)
            .Must(BeNullOrObject)
            .WithMessage("metadata deve ser um objeto JSON quando informado.");
    }

    private static bool BeNullOrObject(JsonElement? metadata) =>
        !metadata.HasValue
        || metadata.Value.ValueKind == JsonValueKind.Object
        || metadata.Value.ValueKind == JsonValueKind.Null
        || metadata.Value.ValueKind == JsonValueKind.Undefined;
}
