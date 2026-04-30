using ECommerce.AgentAPI.Application.Tools;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>
/// Orquestra a aprovação: quais <see cref="KernelFunction"/> requerem confirmação (delegando ao
/// <see cref="ToolCatalog"/>) e acesso ao <see cref="ApprovalStateStore"/> para <b>armazenar</b>
/// / <b>recuperar</b> / <b>limpar</b> a <see cref="PendingToolCall"/> por identificador de sessão,
/// e para marcar o kernel de modo a permitir exatamente uma invocação aprovada (ver
/// <see cref="PrepareApprovedExecution"/> + <see cref="ApprovalFilter"/>).
/// <para>
/// Com a migração do Passo 4 (diagnóstico), a policy de aprovação vive inteiramente em
/// <see cref="ITool.RequiresApproval"/> / <see cref="ITool.BuildApprovalMessage"/> — a lista
/// <c>Agent:RequireApprovalForTools</c> do <c>appsettings</c> e o switch de mensagens sumiram.
/// </para>
/// </summary>
public sealed class ToolApprovalService
{
    private readonly ApprovalStateStore _store;
    private readonly ToolCatalog _catalog;

    public ToolApprovalService(ApprovalStateStore store, ToolCatalog catalog)
    {
        _store = store;
        _catalog = catalog;
    }

    public bool RequiresApproval(string kernelFunctionName) =>
        !string.IsNullOrEmpty(kernelFunctionName) && _catalog.RequiresApproval(kernelFunctionName);

    public bool HasPending(string sessionId) => _store.HasPending(sessionId);

    public void StorePending(string sessionId, PendingToolCall pending) => _store.Store(sessionId, pending);

    public bool TryGetPending(string sessionId, out PendingToolCall? pending) => _store.TryGet(sessionId, out pending);

    public bool TryGetPendingByApprovalId(string sessionId, string approvalId, out PendingToolCall? pending) =>
        _store.TryGetByApprovalId(sessionId, approvalId, out pending);

    public void ClearPending(string sessionId) => _store.Clear(sessionId);

    public void ClearPendingByRawSessionId(string rawSessionId) => _store.ClearByRawSessionId(rawSessionId);

    public bool TryRemovePending(string sessionId, out PendingToolCall? pending) =>
        _store.TryRemove(sessionId, out pending);

    public void PrepareApprovedExecution(Microsoft.SemanticKernel.Kernel kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        kernel.Data[AgentKernelDataKeys.SkipApprovalOnce] = true;
    }

    public string BuildApprovalMessage(string functionName, KernelArguments arguments) =>
        _catalog.BuildApprovalMessage(functionName, ToReadOnly(arguments));

    public PendingToolCall CreatePendingFromInvocation(
        Microsoft.SemanticKernel.KernelFunction function,
        KernelArguments arguments,
        string approvalMessage)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);

        return new PendingToolCall
        {
            ApprovalId = Guid.NewGuid().ToString("D"),
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

    private static IReadOnlyDictionary<string, object?> ToReadOnly(KernelArguments arguments)
    {
        var d = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kv in arguments)
            d[kv.Key] = kv.Value;
        return d;
    }
}
