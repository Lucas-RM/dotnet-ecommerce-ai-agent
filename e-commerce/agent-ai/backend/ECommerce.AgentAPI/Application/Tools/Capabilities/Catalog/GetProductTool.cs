using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Catalog;

/// <summary>
/// <c>get_product</c> — domínio <b>catálogo</b>. Detalhes de produto. Execução em
/// <c>ProductPlugin.GetProductByIdAsync</c>.
/// </summary>
public sealed class GetProductTool : ITool
{
    public string Name => "get_product";
    public string DataType => "Product";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var p = ToolPayloadJson.Deserialize<ProductDataV1>(data);
        var name = p?.Name;
        var intro = string.IsNullOrWhiteSpace(name)
            ? "Aqui estão os detalhes do produto:"
            : $"Detalhes de **{name}**:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer adicionar este produto ao carrinho?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
