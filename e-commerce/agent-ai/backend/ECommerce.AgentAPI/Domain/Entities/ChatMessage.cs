using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Domain.Entities;

/// <summary> Mensagem de conversa agnóstica de Semantic Kernel. </summary>
public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary> Preenchido quando <see cref="Role"/> é <see cref="MessageRole.Tool"/>. </summary>
    public string? ToolName { get; set; }
}
