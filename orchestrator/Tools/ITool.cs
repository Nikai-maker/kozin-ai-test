// Общий интерфейс инструмента: имя для маршрутизации и вызов воркера.

namespace Orchestrator.Tools;

public interface ITool
{
    string Name { get; }
    Task<string> ExecuteAsync(string input);
}
