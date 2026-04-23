using System.Text.Json;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ECommerce.AgentAPI.Infrastructure.Memory;

public sealed class RedisMemoryService : IMemoryService
{
    private const string KeyPrefix = "ecommerce:agent:chat:";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly int _keyTtlSeconds;
    private readonly IConfiguration _configuration;

    public RedisMemoryService(
        IConnectionMultiplexer redis,
        IConfiguration configuration)
    {
        _redis = redis;
        _configuration = configuration;
        _keyTtlSeconds = configuration.GetValue("Memory:Redis:KeyTtlSeconds", 86400);
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(string sessionId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(StorageKey(sessionId)).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
            return [];
        return JsonSerializer.Deserialize<List<ChatMessage>>(value.ToString()!, Json) ?? [];
    }

    public async Task SaveMessageAsync(ChatMessage message)
    {
        var list = await GetHistoryAsync(message.SessionId).ConfigureAwait(false);
        list.Add(message);
        await SetListAsync(message.SessionId, list).ConfigureAwait(false);
    }

    public async Task PruneHistoryAsync(string sessionId, int maxTurns)
    {
        if (maxTurns < 1) maxTurns = 1;
        var list = await GetHistoryAsync(sessionId).ConfigureAwait(false);
        var withSystem = 0;
        for (; withSystem < list.Count && list[withSystem].Role == MessageRole.System; withSystem++)
        {
        }
        var userIndices = new List<int>();
        for (var i = withSystem; i < list.Count; i++)
        {
            if (list[i].Role == MessageRole.User)
                userIndices.Add(i);
        }
        if (userIndices.Count > maxTurns)
        {
            var firstIdx = userIndices[^maxTurns];
            list = [.. list.Take(withSystem), .. list.Skip(firstIdx)];
        }
        await SetListAsync(sessionId, list).ConfigureAwait(false);
    }

    public async Task ClearSessionAsync(string sessionId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(StorageKey(sessionId)).ConfigureAwait(false);
    }

    private string StorageKey(string sessionId) =>
        (_configuration["Memory:Redis:KeyPrefix"]?.Trim() ?? KeyPrefix) + sessionId;

    private async Task SetListAsync(string sessionId, IReadOnlyList<ChatMessage> list)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(list, Json);
        await db
            .StringSetAsync(StorageKey(sessionId), json, TimeSpan.FromSeconds(_keyTtlSeconds))
            .ConfigureAwait(false);
    }
}
