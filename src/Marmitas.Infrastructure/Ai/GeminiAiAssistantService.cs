using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Marmitas.Application.Ai.Dtos;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Marmitas.Infrastructure.Ai;

public class GeminiAiAssistantService : IAiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiAssistantService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
    public string ActiveModel => _model;

    public GeminiAiAssistantService(
        HttpClient httpClient,
        IRestaurantRepository restaurantRepository,
        IConfiguration configuration,
        ILogger<GeminiAiAssistantService> logger)
    {
        _httpClient = httpClient;
        _restaurantRepository = restaurantRepository;
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["Gemini:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? string.Empty;

        _model = _configuration["Gemini:Model"] ?? "gemini-3.8-flash";
    }

    public async Task<MarmitaRecommendationResponse> RecommendMarmitasAsync(
        MarmitaRecommendationRequest request, 
        CancellationToken cancellationToken = default)
    {
        List<Product> products;
        string contextScope;

        if (request.RestaurantId.HasValue && request.RestaurantId.Value != Guid.Empty)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId.Value);
            products = await _restaurantRepository.GetProductsByRestaurantIdAsync(request.RestaurantId.Value);
            contextScope = restaurant != null ? $"no restaurante '{restaurant.Name}'" : "no cardápio selecionado";
        }
        else
        {
            products = await _restaurantRepository.GetAllActiveProductsAsync();
            contextScope = "nas opções disponíveis de marmitas da plataforma";
        }

        if (request.MaxPrice.HasValue && request.MaxPrice.Value > 0)
        {
            products = products.Where(p => p.Price <= request.MaxPrice.Value).ToList();
        }

        if (products.Count == 0)
        {
            return new MarmitaRecommendationResponse
            {
                GeneralAdvice = "Não encontramos marmitas ativas disponíveis com os critérios especificados.",
                Recommendations = new List<MarmitaRecommendationItemDto>(),
                IsLiveAi = false
            };
        }

        // If Gemini API is configured, call Gemini 3.8 Flash
        if (IsConfigured)
        {
            try
            {
                var geminiResponse = await CallGeminiRecommendationAsync(request, products, contextScope, cancellationToken);
                if (geminiResponse != null && geminiResponse.Recommendations.Count > 0)
                {
                    geminiResponse.IsLiveAi = true;
                    return geminiResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar a API do Gemini. Utilizando fallback inteligente.");
            }
        }

        // Fallback inteligente (permite demonstração e funcionamento mesmo sem chave API configurada)
        return GenerateSmartFallbackRecommendation(request, products, contextScope);
    }

    public async Task<GenerateMarmitaDishResponse> GenerateDishDetailsAsync(
        GenerateMarmitaDishRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BaseNameOrIngredients))
        {
            throw new ArgumentException("O nome base ou ingredientes do prato devem ser informados.");
        }

        if (IsConfigured)
        {
            try
            {
                var geminiResponse = await CallGeminiDishGenerationAsync(request, cancellationToken);
                if (geminiResponse != null)
                {
                    geminiResponse.IsLiveAi = true;
                    return geminiResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao gerar detalhes de marmita com Gemini. Utilizando fallback.");
            }
        }

        return GenerateFallbackDish(request);
    }

    private async Task<MarmitaRecommendationResponse?> CallGeminiRecommendationAsync(
        MarmitaRecommendationRequest request,
        List<Product> products,
        string contextScope,
        CancellationToken cancellationToken)
    {
        var menuSnippet = products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            description = p.Description,
            category = p.Category?.Name ?? "Geral",
            price = p.Price
        });

        var systemPrompt = @"Você é um nutricionista e chef consultor especialista da plataforma de marmitas.
Seu objetivo é analisar as marmitas do cardápio e recomendar as melhores opções de acordo com o pedido, objetivo nutricional ou restrição do cliente.
Seja empático, encorajador e forneça estimativas nutricionais aproximadas realistas (calorias, proteínas, carboidratos e gorduras).
IMPORTANTE: Recomende APENAS produtos que estejam na lista enviada, referenciando o id exato de cada um.
Responda SEMPRE em formato JSON estrito conforme o esquema solicitado.";

        var userPrompt = $@"O cliente fez o seguinte pedido {contextScope}:
- Pergunta / Objetivo: ""{request.Query}""
- Meta dietética: ""{request.DietaryGoal ?? "equilíbrio geral"}""
- Preço máximo: {(request.MaxPrice.HasValue ? $"R$ {request.MaxPrice.Value:F2}" : "Sem restrição")}

Cardápio disponível:
{JsonSerializer.Serialize(menuSnippet)}

Retorne um JSON com a estrutura:
{{
  ""generalAdvice"": ""orientação e explicação carinhosa/nutricional para o cliente"",
  ""recommendations"": [
    {{
      ""productId"": ""GUID_DO_PRODUTO"",
      ""productName"": ""Nome do produto"",
      ""price"": 0.00,
      ""whyRecommended"": ""Por que esta marmita é ideal para a meta do cliente"",
      ""nutritionalEstimate"": {{
        ""calories"": 550,
        ""protein"": 38,
        ""carbs"": 45,
        ""fat"": 12
      }},
      ""tags"": [""Alta Proteína"", ""Low Carb"", ""Pós-Treino""]
    }}
  ]
}}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.3
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        using var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, requestContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.GetArrayLength() > 0 &&
            parts[0].TryGetProperty("text", out var textElement))
        {
            var text = textElement.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var result = JsonSerializer.Deserialize<MarmitaRecommendationResponse>(text, JsonOptions);
                if (result != null)
                {
                    // Valida que os produtos recomendados existem na lista original
                    result.Recommendations = result.Recommendations
                        .Where(r => products.Any(p => p.Id == r.ProductId))
                        .ToList();

                    // Preenche campos que porventura faltem
                    foreach (var rec in result.Recommendations)
                    {
                        var original = products.FirstOrDefault(p => p.Id == rec.ProductId);
                        if (original != null)
                        {
                            rec.ProductName = original.Name;
                            rec.Price = original.Price;
                        }
                    }

                    return result;
                }
            }
        }

        return null;
    }

    private async Task<GenerateMarmitaDishResponse?> CallGeminiDishGenerationAsync(
        GenerateMarmitaDishRequest request,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"Você é um chef executivo de culinária saudável e redator gastronômico profissional.
Você ajuda restaurantes de marmitas a transformar ideias simples ou lista de ingredientes em nomes comerciais apetitosos, descrições irresistíveis e fichas nutricionais estimadas.
Responda SEMPRE em formato JSON estrito.";

        var userPrompt = $@"Crie os detalhes gastronômicos para a seguinte marmita:
- Ingredientes ou nome base: ""{request.BaseNameOrIngredients}""
- Categoria pretendida: ""{request.TargetCategory ?? "Tradicionais"}""
- Preço sugerido: {(request.Price.HasValue ? $"R$ {request.Price.Value:F2}" : "a definir")}

Retorne um JSON com a estrutura:
{{
  ""suggestedName"": ""Nome comercial atrativo e profissional para o prato"",
  ""gourmetDescription"": ""Descrição sensorial e apetitosa destacando frescor, tempero e equilíbrio nutricional"",
  ""suggestedCategory"": ""Tradicionais|Fitness & Low Carb|Executiva|Vegetariana & Vegana"",
  ""nutritionalEstimate"": {{
    ""calories"": 520,
    ""protein"": 35,
    ""carbs"": 48,
    ""fat"": 11
  }},
  ""highlightIngredients"": [""Peito de frango grelhado"", ""Alecrim fresco"", ""Azeite extravirgem""]
}}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.5
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        using var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, requestContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.GetArrayLength() > 0 &&
            parts[0].TryGetProperty("text", out var textElement))
        {
            var text = textElement.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return JsonSerializer.Deserialize<GenerateMarmitaDishResponse>(text, JsonOptions);
            }
        }

        return null;
    }

    private static MarmitaRecommendationResponse GenerateSmartFallbackRecommendation(
        MarmitaRecommendationRequest request,
        List<Product> products,
        string contextScope)
    {
        var query = (request.Query ?? string.Empty).ToLowerInvariant();
        var goal = (request.DietaryGoal ?? string.Empty).ToLowerInvariant();

        // Ordena por relevância heurística com base nas palavras-chave
        var sorted = products.OrderByDescending(p =>
        {
            int score = 0;
            var text = $"{p.Name} {p.Description} {p.Category?.Name}".ToLowerInvariant();

            if (query.Contains("prote") || goal.Contains("hipertrofia") || goal.Contains("pos-treino"))
            {
                if (text.Contains("frango") || text.Contains("carne") || text.Contains("patinho") || text.Contains("whey") || text.Contains("ovo")) score += 5;
            }
            if (query.Contains("low carb") || goal.Contains("low carb") || query.Contains("carb"))
            {
                if (text.Contains("low carb") || text.Contains("legumes") || text.Contains("salada") || text.Contains("brócolis")) score += 5;
            }
            if (query.Contains("vege") || query.Contains("vegan") || goal.Contains("vegetariano"))
            {
                if (text.Contains("vege") || text.Contains("tofu") || text.Contains("cogumelo") || text.Contains("grão de bico")) score += 6;
            }
            if (query.Contains("leve") || query.Contains("salada"))
            {
                if (text.Contains("salada") || text.Contains("leve") || text.Contains("peixe")) score += 4;
            }
            return score;
        }).Take(3).ToList();

        var recommendations = sorted.Select(p => new MarmitaRecommendationItemDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Price = p.Price,
            WhyRecommended = $"Excelente escolha {contextScope}, combinando equilíbrio nutricional, ingredientes frescos e ótimo custo-benefício para sua rotina.",
            NutritionalEstimate = new NutritionalEstimateDto
            {
                Calories = 480 + (int)(p.Price % 10) * 15,
                Protein = 32 + (int)(p.Price % 5) * 2,
                Carbs = 42,
                Fat = 11
            },
            Tags = new List<string> { "Equilibrada", "Ingredientes Frescos", "Mais Pedida" }
        }).ToList();

        return new MarmitaRecommendationResponse
        {
            GeneralAdvice = "Sugestões selecionadas para o seu perfil alimentar. (Dica: defina sua chave GEMINI_API_KEY no appsettings.json para ativar análises nutricionais ao vivo com Gemini 3.8 Flash).",
            Recommendations = recommendations,
            IsLiveAi = false
        };
    }

    private static GenerateMarmitaDishResponse GenerateFallbackDish(GenerateMarmitaDishRequest request)
    {
        var raw = request.BaseNameOrIngredients.Trim();
        var capitalized = char.ToUpper(raw[0]) + raw[1..];

        return new GenerateMarmitaDishResponse
        {
            SuggestedName = $"{capitalized} Especial do Chef",
            GourmetDescription = $"{capitalized}, preparado artesanalmente com ingredientes selecionados, tempero natural aromático e cozimento no ponto ideal para preservar nutrientes e sabor.",
            SuggestedCategory = string.IsNullOrWhiteSpace(request.TargetCategory) ? "Tradicionais" : request.TargetCategory,
            NutritionalEstimate = new NutritionalEstimateDto
            {
                Calories = 510,
                Protein = 34,
                Carbs = 44,
                Fat = 12
            },
            HighlightIngredients = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsLiveAi = false
        };
    }
}
