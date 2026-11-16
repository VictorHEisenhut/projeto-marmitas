using Marmitas.Application.Ai.Dtos;
using Marmitas.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _aiAssistantService;

    public AiController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    /// <summary>
    /// Retorna o status de configuração do Gemini API e o modelo ativo.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isConfigured = _aiAssistantService.IsConfigured,
            model = _aiAssistantService.ActiveModel,
            provider = "Google Gemini"
        });
    }

    /// <summary>
    /// Recomenda marmitas com base em critérios nutricionais e preferências do cliente.
    /// </summary>
    [HttpPost("recommend-marmitas")]
    [AllowAnonymous]
    public async Task<IActionResult> RecommendMarmitas(
        [FromBody] MarmitaRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) && string.IsNullOrWhiteSpace(request.DietaryGoal))
        {
            return BadRequest(new { message = "Informe uma pergunta ou objetivo nutricional." });
        }

        var result = await _aiAssistantService.RecommendMarmitasAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gera descrição gourmet, sugestão de categoria e estimativa nutricional para restaurantes parceiros.
    /// </summary>
    [HttpPost("generate-marmita")]
    [Authorize(Roles = "admin,super_admin,restaurant_owner,restaurant_staff")]
    public async Task<IActionResult> GenerateMarmitaDetails(
        [FromBody] GenerateMarmitaDishRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BaseNameOrIngredients))
        {
            return BadRequest(new { message = "Informe o nome ou ingredientes base da marmita." });
        }

        var result = await _aiAssistantService.GenerateDishDetailsAsync(request, cancellationToken);
        return Ok(result);
    }
}
