// Реестр инструментов: поиск ITool по имени, выбранному LLM.

using Orchestrator.Tools;

namespace Orchestrator.Orchestrator;

public class ToolRouter
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRouter(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, t => t);  // {t.name: t for t in tools}
    }

    public ITool? GetTool(string toolName)
    {
        return _tools.GetValueOrDefault(toolName.ToLowerInvariant());
    }

    public IEnumerable<string> GetAvailableTools()
    {
        return _tools.Keys;
    }
}
