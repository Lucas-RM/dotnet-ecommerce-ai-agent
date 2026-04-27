using System.Globalization;

namespace ECommerce.AgentAPI.Application.Tools.Shared;

public static class ToolEnvelopeText
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static string FormatMoney(decimal? value) =>
        value.HasValue ? value.Value.ToString("C", PtBr) : "—";
}
