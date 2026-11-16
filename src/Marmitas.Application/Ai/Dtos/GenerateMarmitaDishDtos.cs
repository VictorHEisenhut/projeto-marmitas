namespace Marmitas.Application.Ai.Dtos;

public class GenerateMarmitaDishRequest
{
    public string BaseNameOrIngredients { get; set; } = string.Empty;
    public string? TargetCategory { get; set; }
    public decimal? Price { get; set; }
}

public class GenerateMarmitaDishResponse
{
    public string SuggestedName { get; set; } = string.Empty;
    public string GourmetDescription { get; set; } = string.Empty;
    public string SuggestedCategory { get; set; } = string.Empty;
    public NutritionalEstimateDto NutritionalEstimate { get; set; } = new();
    public List<string> HighlightIngredients { get; set; } = new();
    public bool IsLiveAi { get; set; }
}
