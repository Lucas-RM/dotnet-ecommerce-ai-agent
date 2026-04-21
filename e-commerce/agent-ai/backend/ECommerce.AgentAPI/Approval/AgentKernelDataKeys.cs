namespace ECommerce.AgentAPI.Approval;

/// <summary>Chaves em <see cref="Microsoft.SemanticKernel.Kernel.Data"/> para o fluxo de aprovação.</summary>
public static class AgentKernelDataKeys
{
    /// <summary>Identificador da sessão do chat (mesmo valor de <see cref="Models.ChatRequest.SessionId"/>).</summary>
    public const string SessionId = "Agent.SessionId";

    /// <summary>
    /// Quando <c>true</c>, a próxima invocação de tool que normalmente exigiria aprovação é executada sem bloquear
    /// (ex.: após confirmação explícita no middleware).
    /// </summary>
    public const string SkipApprovalOnce = "Agent.SkipApprovalOnce";
}
