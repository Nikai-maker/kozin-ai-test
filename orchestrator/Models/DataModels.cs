// DTO-модели: контракты данных между оркестратором, воркерами и Ollama.

namespace Orchestrator.Models;

public class PredictionResult
{
    public string Label { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class ModelPrediction
{
    public string Model { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class VotingResult
{
    public string FinalLabel { get; set; } = string.Empty;
    public Dictionary<string, int> Votes { get; set; } = new();
    public List<ModelPrediction> Predictions { get; set; } = new();
}

public class OrchestratorDecision
{
    public string Tool { get; set; } = string.Empty;
}

public class LlmRequest
{
    public string Model { get; set; } = "llama3.2:3b";
    public string Prompt { get; set; } = string.Empty;
    public bool Stream { get; set; } = false;
    public string? Format { get; set; }
}

public class LlmResponse
{
    public string Response { get; set; } = string.Empty;
}
