using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using DomainChat = ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Infrastructure.Memory;

public sealed class VolatileMemoryService : IMemoryService
{
    private readonly AgentMemoryStore _store;

    public VolatileMemoryService(AgentMemoryStore store) => _store = store;

    public Task<List<DomainChat.ChatMessage>> GetHistoryAsync(string sessionId)
    {
        var history = _store.GetOrCreate(sessionId);
        var list = new List<DomainChat.ChatMessage>();
        foreach (var c in history)
        {
            list.Add(new DomainChat.ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = ToDomainRole(c.Role),
                Content = c.Content ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                ToolName = null
            });
        }
        return Task.FromResult(list);
    }

    public Task SaveMessageAsync(DomainChat.ChatMessage message)
    {
        var history = _store.GetOrCreate(message.SessionId);
        var content = message.Content;
        switch (message.Role)
        {
            case MessageRole.User:
                history.AddUserMessage(content);
                break;
            case MessageRole.Assistant:
                history.AddAssistantMessage(content);
                break;
            case MessageRole.System:
                if (!history.Any(m => m.Role == AuthorRole.System))
                    history.Insert(0, new ChatMessageContent(AuthorRole.System, content));
                else
                    history[0] = new ChatMessageContent(AuthorRole.System, content);
                break;
            case MessageRole.Tool:
                history.AddAssistantMessage(string.IsNullOrEmpty(message.ToolName) ? content : $"[{message.ToolName}] {content}");
                break;
        }
        return Task.CompletedTask;
    }

    public Task PruneHistoryAsync(string sessionId, int maxTurns)
    {
        _store.PruneTo(sessionId, maxTurns);
        return Task.CompletedTask;
    }

    public Task ClearSessionAsync(string sessionId)
    {
        _store.RemoveSession(sessionId);
        return Task.CompletedTask;
    }

    private static MessageRole ToDomainRole(AuthorRole r)
    {
        if (r == AuthorRole.User) return MessageRole.User;
        if (r == AuthorRole.Assistant) return MessageRole.Assistant;
        if (r == AuthorRole.System) return MessageRole.System;
        if (r == AuthorRole.Tool) return MessageRole.Tool;
        return MessageRole.User;
    }
}
