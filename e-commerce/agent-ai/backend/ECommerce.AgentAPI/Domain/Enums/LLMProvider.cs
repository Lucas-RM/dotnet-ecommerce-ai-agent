namespace ECommerce.AgentAPI.Domain.Enums;

/// <summary>Provedor LLM configurável (<c>LLM:Provider</c> em appsettings). Camada de domínio — sem referências a SK/Ollama.</summary>
public enum LLMProvider
{
    OpenAI = 0,
    Ollama = 1
}
