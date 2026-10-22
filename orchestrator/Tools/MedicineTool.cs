// Инструмент medicine: отправляет симптомы в медицинский воркер (порт 8003) и форматирует результат анализа.

using System.Text.Json;
using Orchestrator.Models;

namespace Orchestrator.Tools;

public class MedicineTool : ITool
{
    private readonly HttpClient _httpClient;
    private const string WorkerUrl = "http://localhost:8003";

    public string Name => "medicine";

    public MedicineTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ExecuteAsync(string input)
    {
        Console.WriteLine($"[MEDICINE-TOOL] Вызываю воркер по адресу {WorkerUrl}");

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

        var finalAnswer = $"[MEDICINE-TOOL] Анализ медицинских симптомов: {result?.Label} (уверенность:{result?.Score:F2})";
        Console.WriteLine(finalAnswer);
        return finalAnswer;
    }
}
