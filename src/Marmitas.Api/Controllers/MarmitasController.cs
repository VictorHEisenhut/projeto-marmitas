using Marmitas.Application.Restaurants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class MarmitasController : ControllerBase
{
    private static readonly List<ProductOptionDto> Options = new();
    private readonly IRestaurantQueryService _restaurantQueryService;

    public MarmitasController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet("api/marmitas")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var products = await _restaurantQueryService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("api/marmitas/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _restaurantQueryService.GetProductByIdAsync(id);
        return Ok(product);
    }

    [HttpPost("api/restaurantes/{id}/marmitas")]
    [Authorize]
    public async Task<IActionResult> AddToMenu(Guid id, [FromBody] CreateProductRequest request)
    {
        var result = await _restaurantQueryService.CreateProductAsync(id, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/marmitas/{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProductRequest request)
    {
        var result = await _restaurantQueryService.UpdateProductAsync(id, request);
        return Ok(result);
    }

    [HttpPatch("api/marmitas/{id}/disponibilidade")]
    [Authorize]
    public async Task<IActionResult> ChangeAvailability(Guid id, [FromBody] UpdateProductAvailabilityRequest request)
    {
        var result = await _restaurantQueryService.ChangeProductAvailabilityAsync(id, request.IsActive);
        return Ok(result);
    }

    [HttpDelete("api/marmitas/{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _restaurantQueryService.DeleteProductAsync(id);
        return NoContent();
    }

    [HttpGet("api/marmitas/{id}/opcoes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOptions(Guid id)
    {
        await _restaurantQueryService.GetProductByIdAsync(id);
        var list = Options.Where(o => o.ProductId == id).ToList();
        return Ok(list);
    }

    [HttpPost("api/marmitas/{id}/opcoes")]
    [Authorize]
    public async Task<IActionResult> AddOption(Guid id, [FromBody] SaveProductOptionRequest request)
    {
        await _restaurantQueryService.GetProductByIdAsync(id);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome da opção é obrigatório.");

        var option = new ProductOptionDto
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            Name = request.Name.Trim(),
            AdditionalPrice = request.AdditionalPrice
        };

        Options.Add(option);
        return StatusCode(201, option);
    }

    [HttpPut("api/marmitas/opcoes/{id}")]
    [Authorize]
    public IActionResult UpdateOption(Guid id, [FromBody] SaveProductOptionRequest request)
    {
        var option = Options.FirstOrDefault(o => o.Id == id);
        if (option == null)
            throw new Exception("Opção não encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            option.Name = request.Name.Trim();
        option.AdditionalPrice = request.AdditionalPrice;

        return Ok(option);
    }

    [HttpDelete("api/marmitas/opcoes/{id}")]
    [Authorize]
    public IActionResult DeleteOption(Guid id)
    {
        var option = Options.FirstOrDefault(o => o.Id == id);
        if (option == null)
            throw new Exception("Opção não encontrada.");

        Options.Remove(option);
        return NoContent();
    }
}
