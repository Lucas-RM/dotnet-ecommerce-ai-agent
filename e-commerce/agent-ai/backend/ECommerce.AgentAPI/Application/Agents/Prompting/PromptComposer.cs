using ECommerce.AgentAPI.Application.Agents.Profiles;
using Microsoft.Extensions.Hosting;

namespace ECommerce.AgentAPI.Application.Agents.Prompting;

public sealed class PromptComposer : IPromptComposer
{
    private readonly IHostEnvironment _environment;

    public PromptComposer(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ComposeSystemPrompt(IAgentProfile profile)
    {
        var relativePath = profile.PromptTemplate?.Trim();
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        var fullPath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_environment.ContentRootPath, relativePath);

        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
    }
}
