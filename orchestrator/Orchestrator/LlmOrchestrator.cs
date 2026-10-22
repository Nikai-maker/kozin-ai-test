// Основной пайплайн: LLM выбирает инструмент -> вызывается воркер -> LLM формулирует финальный ответ (Ollama).

using System.Text.Json;
using Orchestrator.Models;

namespace Orchestrator.Orchestrator;

public class LlmOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly ToolRouter _toolRouter;
    private const string OllamaUrl = "http://localhost:11434";
    private const string ModelName = "llama3.2:3b";

    public LlmOrchestrator(HttpClient httpClient, ToolRouter toolRouter)
    {
        _httpClient = httpClient;
        _toolRouter = toolRouter;
    }

    public async Task<string> ProcessRequestAsync(string userInput)
    {
        Console.WriteLine($"[ORCHESTRATOR] Запрос пользователя: '{userInput}'");

        // Шаг 1: попросить LLM выбрать инструмент
        var toolSelection = await SelectToolAsync(userInput);

        if (string.IsNullOrEmpty(toolSelection))
        {
            return "Не знаю, какой инструмент использовать для вашего запроса(";
        }

        Console.WriteLine($"[ORCHESTRATOR] Выбранный инструмент: {toolSelection}");

        var tool = _toolRouter.GetTool(toolSelection);
        if (tool == null)
        {
            return $"Ошибка: Инструмент '{toolSelection}' не найден. Доступные инструменты: {string.Join(", ", _toolRouter.GetAvailableTools())}";
        }

        // Шаг 2: Выполнить инструмент
        string toolResult;
        try
        {
            toolResult = await tool.ExecuteAsync(userInput);
            Console.WriteLine($"[ORCHESTRATOR] Результат инструмента: {toolResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORCHESTRATOR] Выполнение инструмента завершилось ошибкой: {ex.Message}");
            return $"Ошибка выполнения: {toolSelection} tool: {ex.Message}";
        }

        // Шаг 3: Просим LLM сформировать финальный ответ
        Console.WriteLine("[ORCHESTRATOR] Отправляю результат воркера в LLM");
        var finalAnswer = await GenerateFinalAnswerAsync(userInput, toolResult);

        Console.WriteLine($"[ORCHESTRATOR] Финальный ответ: {finalAnswer}");

        return finalAnswer;
    }

    private async Task<string> SelectToolAsync(string userInput)
    {
        var prompt = $@"You are a tool selector. Given a user question, select ONE tool from: physics, mathematics, medicine.

User question: ""{userInput}""

Rules:
- Select 'physics' for questions about physical phenomena, forces, electricity, magnetism, waves, thermal, nuclear physics
- Select 'mathematics' for math problems, algebra, geometry, probability, number theory, calculus
- Select 'medicine' for medical symptoms, health conditions, diseases

Respond ONLY with a JSON object in this exact format:
{{""tool"": ""<toolname>""}}

Where <toolname> is one of: physics, mathematics, medicine";

        try
        {
            var response = await CallLlmAsync(prompt, useJsonFormat: true);

            // Разбор JSON-ответа
            var decision = JsonSerializer.Deserialize<OrchestratorDecision>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return decision?.Tool ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORCHESTRATOR] Не удалось выбрать инструмент:{ex.Message}");
            return string.Empty;
        }
    }

    private async Task<string> GenerateFinalAnswerAsync(string userInput, string toolResult)
    {
        var prompt = $@"You are a helpful assistant. A user asked a question, and we got a result from a specialized tool.
        Tools are supposed to answer what topic of phisics or mathematics the user question is about. Or what medical condition there is accoding symptomps  

User question: ""{userInput}""

Tool result: ""{toolResult}""

Based on the tool result, answer user shortly what the topic his question is about or what medical condition he has. Answer in Russian.";

        try
        {
            return await CallLlmAsync(prompt, useJsonFormat: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORCHESTRATOR] Не удалось сформировать финальный ответ: {ex.Message}");
            return $"Результат инструмента: {toolResult}";
        }
    }

    private async Task<string> CallLlmAsync(string prompt, bool useJsonFormat)
    {
        var request = new LlmRequest
        {
            Model = ModelName,
            Prompt = prompt,
            Stream = false,
            Format = useJsonFormat ? "json" : null
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{OllamaUrl}/api/generate", content);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Ollama вернула {(int)response.StatusCode}: {responseJson}");
        }

        var llmResponse = JsonSerializer.Deserialize<LlmResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return llmResponse?.Response ?? string.Empty;
    }
}
