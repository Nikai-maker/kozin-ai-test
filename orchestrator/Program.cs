// Точка входа: проверка Ollama, сборка инструментов и консольный цикл вопрос-ответ.

using Orchestrator.Orchestrator;
using Orchestrator.Tools;

namespace Orchestrator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ИИ оркестратор MVP ===");
        Console.WriteLine("Проект демонстрации паттернов использования ИИ: Orch-workers, Voiting");
        Console.WriteLine();

        // Проверяем, что ollama запущена
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        try
        {
            Console.WriteLine("[ORCHESTRATOR] Проверяем запуск ollama...");
            var ollamaCheck = await httpClient.GetAsync("http://localhost:11434/api/tags");
            if (!ollamaCheck.IsSuccessStatusCode)
            {
                Console.WriteLine("[ERROR] Ollama не доступна. Пожалуйста перезагрузи Ollama");
                Console.WriteLine("Запусти: ollama serve");
                return;
            }
            Console.WriteLine("[ORCHESTRATOR] Ollama подключена!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Не получается подключиться к Ollama. Ошибка: {ex.Message}");
            Console.WriteLine("Проверь, что Ollama запущена через: ollama serve");
            return;
        }

        var tools = new List<ITool> // Список объектов-наследников ITool
        {
            new PhysicsTool(httpClient),
            new MathematicsTool(httpClient),
            new MedicineTool(httpClient)
        };

        var toolRouter = new ToolRouter(tools);
        var orchestrator = new LlmOrchestrator(httpClient, toolRouter);

        Console.WriteLine($"[ORCHESTRATOR] Доступные инструменты: {string.Join(", ", toolRouter.GetAvailableTools())}");
        Console.WriteLine();
        Console.WriteLine("Напишите свой запрос или напишите exit чтобы выйти");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Пользователь: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Trim().ToLowerInvariant() == "exit")
            {
                Console.WriteLine("Завершение программы...");
                break;
            }

            try
            {
                var answer = await orchestrator.ProcessRequestAsync(input);
                Console.WriteLine($"\nАссистент: {answer}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] {ex.Message}\n");
            }
        }
    }
}
