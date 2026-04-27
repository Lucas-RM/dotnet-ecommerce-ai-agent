using System.Text.RegularExpressions;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

public static class ToolContractValidator
{
    private static readonly Regex SemVer = new(
        @"^\d+\.\d+\.\d+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static void ValidateOrThrow(ToolCatalog catalog, IReadOnlyList<ToolDefinition> contracts)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(contracts);

        var errors = new List<string>();
        var contractNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var contract in contracts)
        {
            ValidateContractShape(contract, errors);

            if (!contractNames.Add(contract.Name))
            {
                errors.Add($"Tool '{contract.Name}' aparece duplicada no contrato unificado.");
            }
        }

        foreach (var tool in catalog.GetAll())
        {
            if (!contractNames.Contains(tool.Name))
            {
                errors.Add($"Tool '{tool.Name}' existe no catálogo, mas não foi descoberta como KernelFunction.");
            }
        }

        foreach (var contract in contracts)
        {
            if (!catalog.Contains(contract.Name))
            {
                errors.Add($"KernelFunction '{contract.Name}' não possui implementação ITool no catálogo.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Validação do contrato de tools falhou:\n - " + string.Join("\n - ", errors));
        }
    }

    private static void ValidateContractShape(ToolDefinition contract, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(contract.Name))
            errors.Add("Existe tool com nome vazio.");

        if (string.IsNullOrWhiteSpace(contract.Description))
            errors.Add($"Tool '{contract.Name}' sem descrição.");

        if (string.IsNullOrWhiteSpace(contract.Version) || !SemVer.IsMatch(contract.Version))
            errors.Add($"Tool '{contract.Name}' com versão inválida '{contract.Version}'. Use SemVer (x.y.z).");

        foreach (var parameter in contract.Parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
                errors.Add($"Tool '{contract.Name}' possui parâmetro com nome vazio.");

            if (string.IsNullOrWhiteSpace(parameter.Type))
                errors.Add($"Tool '{contract.Name}' possui parâmetro '{parameter.Name}' sem tipo.");
        }
    }
}
