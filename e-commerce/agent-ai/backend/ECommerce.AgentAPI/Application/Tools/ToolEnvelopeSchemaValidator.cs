using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

public sealed class ToolEnvelopeSchemaValidator
{
    public bool TryValidate(ChatEnvelope envelope, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(envelope.DataType))
            return true;

        if (envelope.Data is null)
        {
            error = $"data ausente para dataType '{envelope.DataType}'.";
            return false;
        }

        var data = envelope.Data.Value;
        if (data.ValueKind != JsonValueKind.Object)
        {
            error = $"data inválido para dataType '{envelope.DataType}': esperado objeto JSON.";
            return false;
        }

        return envelope.DataType switch
        {
            "PagedProducts" => ValidatePagedPayload(data, envelope.DataType, out error),
            "PagedOrders" => ValidatePagedPayload(data, envelope.DataType, out error),
            "Product" => ValidateProductPayload(data, out error),
            "Cart" => ValidateCartPayload(data, out error),
            "Order" => ValidateOrderPayload(data, out error),
            _ => true
        };
    }

    private static bool ValidatePagedPayload(JsonElement data, string dataType, out string? error)
    {
        if (!HasPropertyKind(data, "items", JsonValueKind.Array))
        {
            error = $"schema inválido em '{dataType}': campo 'items' ausente ou não-array.";
            return false;
        }

        if (!HasPropertyKind(data, "totalCount", JsonValueKind.Number))
        {
            error = $"schema inválido em '{dataType}': campo 'totalCount' ausente ou não-numérico.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateProductPayload(JsonElement data, out string? error)
    {
        if (!HasProperty(data, "id"))
        {
            error = "schema inválido em 'Product': campo 'id' ausente.";
            return false;
        }

        if (!HasPropertyKind(data, "name", JsonValueKind.String))
        {
            error = "schema inválido em 'Product': campo 'name' ausente ou inválido.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateCartPayload(JsonElement data, out string? error)
    {
        if (!HasPropertyKind(data, "items", JsonValueKind.Array))
        {
            error = "schema inválido em 'Cart': campo 'items' ausente ou não-array.";
            return false;
        }

        if (!HasProperty(data, "totalPrice"))
        {
            error = "schema inválido em 'Cart': campo 'totalPrice' ausente.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateOrderPayload(JsonElement data, out string? error)
    {
        if (!HasProperty(data, "id"))
        {
            error = "schema inválido em 'Order': campo 'id' ausente.";
            return false;
        }

        if (!HasProperty(data, "status"))
        {
            error = "schema inválido em 'Order': campo 'status' ausente.";
            return false;
        }

        if (!HasProperty(data, "totalAmount"))
        {
            error = "schema inválido em 'Order': campo 'totalAmount' ausente.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool HasProperty(JsonElement data, string propertyName) =>
        data.TryGetProperty(propertyName, out _);

    private static bool HasPropertyKind(JsonElement data, string propertyName, JsonValueKind expectedKind) =>
        data.TryGetProperty(propertyName, out var prop) && prop.ValueKind == expectedKind;
}
