using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Approval;

/// <summary>
/// Regras de quais tools exigem aprovação (config) e operações de armazenar / recuperar / limpar pendentes.
/// </summary>
public sealed class ToolApprovalService
{
    private readonly ApprovalStateStore _store;
    private readonly HashSet<string> _requiresApproval;

    public ToolApprovalService(ApprovalStateStore store, IConfiguration configuration)
    {
        _store = store;
        var list = configuration.GetSection("Agent:RequireApprovalForTools").Get<string[]>() ?? [];
        _requiresApproval = new HashSet<string>(list, StringComparer.Ordinal);
    }

    /// <summary>Nome da função no kernel (ex.: <c>add_cart_item</c>), conforme <c>[KernelFunction("...")]</c>.</summary>
    public bool RequiresApproval(string kernelFunctionName) =>
        !string.IsNullOrEmpty(kernelFunctionName) && _requiresApproval.Contains(kernelFunctionName);

    public bool HasPending(string sessionId) => _store.HasPending(sessionId);

    public void StorePending(string sessionId, PendingToolCall pending) => _store.Store(sessionId, pending);

    public bool TryGetPending(string sessionId, out PendingToolCall? pending) => _store.TryGet(sessionId, out pending);

    public void ClearPending(string sessionId) => _store.Clear(sessionId);

    public bool TryRemovePending(string sessionId, out PendingToolCall? pending) =>
        _store.TryRemove(sessionId, out pending);

    /// <summary>
    /// Marca o <paramref name="kernel"/> para executar uma única invocação de tool sem bloqueio de aprovação.
    /// </summary>
    public void PrepareApprovedExecution(Microsoft.SemanticKernel.Kernel kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        kernel.Data[AgentKernelDataKeys.SkipApprovalOnce] = true;
    }

    /// <summary>Monta a mensagem exibida ao usuário quando a tool foi bloqueada.</summary>
    public string BuildApprovalMessage(string functionName, KernelArguments arguments)
    {
        var productId = GetArg(arguments, "productId") ?? GetArg(arguments, "ProductId");
        var quantityStr = GetArg(arguments, "quantity") ?? GetArg(arguments, "Quantity");
        _ = int.TryParse(quantityStr, out var quantity);

        return functionName switch
        {
            "add_cart_item" =>
                $"Deseja adicionar o produto **{FormatProductRef(productId)}** — quantidade: **{quantity}** — ao seu carrinho? Responda **sim** para confirmar ou **não** para cancelar.",

            "update_cart_item" =>
                $"Deseja atualizar a quantidade do produto **{FormatProductRef(productId)}** para **{quantity}** unidade(s)? Responda **sim** para confirmar ou **não** para cancelar.",

            "remove_cart_item" =>
                $"Deseja remover **{FormatProductRef(productId)}** do seu carrinho? Responda **sim** para confirmar ou **não** para cancelar.",

            "clear_cart" =>
                "Tem certeza que deseja **esvaziar todo o seu carrinho**? Esta ação removerá todos os itens adicionados. Responda **sim** para confirmar ou **não** para cancelar.",

            "checkout" =>
                "Deseja **finalizar o pedido (checkout)**? Responda **sim** para confirmar ou **não** para cancelar.",

            _ =>
                $"Confirme a ação **{functionName}** respondendo **sim** ou **não**."
        };
    }

    public PendingToolCall CreatePendingFromInvocation(
        Microsoft.SemanticKernel.KernelFunction function,
        KernelArguments arguments,
        string approvalMessage)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);

        return new PendingToolCall
        {
            PluginName = function.PluginName ?? string.Empty,
            FunctionName = function.Name,
            Arguments = CloneArguments(arguments),
            ApprovalMessage = approvalMessage,
            StoredAt = DateTimeOffset.UtcNow
        };
    }

    public static KernelArguments CloneArguments(KernelArguments source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var clone = new KernelArguments();
        foreach (var kv in source)
            clone[kv.Key] = kv.Value;
        return clone;
    }

    private static string? GetArg(KernelArguments arguments, string key) =>
        arguments.TryGetValue(key, out var v) ? v?.ToString() : null;

    private static string FormatProductRef(string? productId) =>
        string.IsNullOrWhiteSpace(productId) ? "(produto não informado)" : productId;
}
