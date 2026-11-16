namespace Marmitas.Application.Ai.Dtos;

public class MarmitaRecommendationRequest
{
    public Guid? RestaurantId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string? DietaryGoal { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class MarmitaRecommendationItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? RestaurantName { get; set; }
    public string WhyRecommended { get; set; } = string.Empty;
    public NutritionalEstimateDto NutritionalEstimate { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class MarmitaRecommendationResponse
{
    public string GeneralAdvice { get; set; } = string.Empty;
    public List<MarmitaRecommendationItemDto> Recommendations { get; set; } = new();
    public bool IsLiveAi { get; set; }
}
