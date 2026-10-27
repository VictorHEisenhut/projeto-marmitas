using Marmitas.Application.Restaurants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/categorias")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public CategoriesController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] Guid? restaurantId)
    {
        var categories = await _restaurantQueryService.GetCategoriesAsync(restaurantId);
        return Ok(categories);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _restaurantQueryService.CreateCategoryAsync(request);
        return StatusCode(201, result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCategoryRequest request)
    {
        var result = await _restaurantQueryService.UpdateCategoryAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _restaurantQueryService.DeleteCategoryAsync(id);
        return NoContent();
    }
}
