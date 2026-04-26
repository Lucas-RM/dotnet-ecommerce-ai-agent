using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>get_product</c> — detalhes completos de um produto. Sem aprovação; envelope usa <c>name</c>
/// quando disponível no intro. Execução em <c>ProductPlugin.GetProductByIdAsync</c>.
/// </summary>
public sealed class GetProductTool : ITool
{
    public string Name => "get_product";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var name = EnvelopeJson.GetString(data, "name");
        var intro = string.IsNullOrWhiteSpace(name)
            ? "Aqui estão os detalhes do produto:"
            : $"Detalhes de **{name}**:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer adicionar este produto ao carrinho?",
            ToolName: Name,
            DataType: "Product",
            Data: data);
    }
}
