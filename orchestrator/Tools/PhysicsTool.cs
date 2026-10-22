// Инструмент physics: отправляет вопрос в физический воркер (порт 8001) и форматирует его классификацию.

using System.Text.Json;
using Orchestrator.Models;

namespace Orchestrator.Tools;

public class PhysicsTool : ITool
{
    private readonly HttpClient _httpClient;
    private const string WorkerUrl = "http://localhost:8001";

    public string Name => "physics";
    // public string Name {
    //     get
    //     {
    //         return "physics";
    //     }
    // }

    public PhysicsTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ExecuteAsync(string input)
    {
        Console.WriteLine($"[PHYSICS-TOOL] Вызываю воркер по адресу {WorkerUrl}");

        var request = new { text = input };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{WorkerUrl}/predict", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PredictionResult>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return $"Классификация физика: {result?.Label} (уверенность: {result?.Score:F2})";
    }
}
