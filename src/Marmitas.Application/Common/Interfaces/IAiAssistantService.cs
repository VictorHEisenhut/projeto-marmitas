using Marmitas.Application.Ai.Dtos;

namespace Marmitas.Application.Common.Interfaces;

public interface IAiAssistantService
{
    bool IsConfigured { get; }
    string ActiveModel { get; }
    Task<MarmitaRecommendationResponse> RecommendMarmitasAsync(MarmitaRecommendationRequest request, CancellationToken cancellationToken = default);
    Task<GenerateMarmitaDishResponse> GenerateDishDetailsAsync(GenerateMarmitaDishRequest request, CancellationToken cancellationToken = default);
}
