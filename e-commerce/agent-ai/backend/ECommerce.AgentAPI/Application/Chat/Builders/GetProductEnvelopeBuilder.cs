using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class GetProductEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "get_product";

    public ChatEnvelope Build(JsonElement? data)
    {
        var name = EnvelopeJson.GetString(data, "name");
        var intro = string.IsNullOrWhiteSpace(name)
            ? "Aqui estão os detalhes do produto:"
            : $"Detalhes de **{name}**:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer adicionar este produto ao carrinho?",
            ToolName: ToolName,
            DataType: "Product",
            Data: data);
    }
}
