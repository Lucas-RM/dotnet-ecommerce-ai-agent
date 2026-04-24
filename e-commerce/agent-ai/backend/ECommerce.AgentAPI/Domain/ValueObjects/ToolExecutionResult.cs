using System.Text.Json;

namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class ToolExecutionResult
{
    public bool Success { get; set; }

    /// <summary>Saída bruta da tool (JSON serializado). Mantida para logs, memória e fallback textual.</summary>
    public string Output { get; set; } = string.Empty;

    public string? Error { get; set; }

    /// <summary>Nome da tool executada (ex.: <c>search_products</c>). Usado pelo registry de envelopes.</summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Dados estruturados já desembrulhados do envelope <c>{success, data, errors}</c> da API do e-commerce.
    /// Fonte da verdade para a UI. <c>null</c> em erros ou quando a tool não produz dados apresentáveis.
    /// </summary>
    public JsonElement? Data { get; set; }

    public static ToolExecutionResult NotFound(string toolName) =>
        new()
        {
            Success = false,
            Output = string.Empty,
            Error = $"Tool desconhecida: {toolName}",
            ToolName = toolName,
            Data = null
        };
}
