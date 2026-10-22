// Инструмент mathematics: отправляет вопрос в математический воркер (порт 8002), который возвращает результат голосования трёх моделей.

using System.Text.Json;
using Orchestrator.Models;

namespace Orchestrator.Tools;

public class MathematicsTool : ITool
{
    private readonly HttpClient _httpClient;
    private const string WorkerUrl = "http://localhost:8002";

    public string Name => "mathematics";

    public MathematicsTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ExecuteAsync(string input)
    {
        Console.WriteLine($"[MATHEMATICS-TOOL] Вызываю воркер по адресу {WorkerUrl}");

        var request = new { text = input };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{WorkerUrl}/predict", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<VotingResult>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null) return "Ошибка: математический воркер не вернул результат";

        Console.WriteLine("[MATHEMATICS-TOOL] Предсказания:");
        foreach (var pred in result.Predictions)
        {
            Console.WriteLine($"[MATHEMATICS-TOOL]     {pred.Model} -> {pred.Label}");
        }

        Console.WriteLine($"[MATHEMATICS-TOOL] Результат голосования:{result.FinalLabel} ({string.Join(", ", result.Votes.Select(v => $"{v.Key}: {v.Value}"))})");

        return $"Классификация математики: {result.Predictions.First().Label} (голоса:{result.Votes[result.Predictions.First().Label]}/3)"; 
    }
}