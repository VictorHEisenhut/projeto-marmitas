using Marmitas.Application.Restaurants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/restaurantes")]
[ApiController]
[AllowAnonymous]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public RestaurantsController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var restaurants = await _restaurantQueryService.GetAllPublicAsync();
        return Ok(restaurants);
    }

    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);
    [HttpPost] public IActionResult Create() => StatusCode(501);
    [HttpPut("{id}")] public IActionResult Update(Guid id) => StatusCode(501);
    [HttpPatch("{id}/status")] public IActionResult ChangeStatus(Guid id) => StatusCode(501);
    [HttpDelete("{id}")] public IActionResult Delete(Guid id) => StatusCode(501);

    [HttpGet("{id}/cardapio")]
    public async Task<IActionResult> GetProducts(Guid id)
    {
        var products = await _restaurantQueryService.GetRestaurantProductsAsync(id);
        return Ok(products);
    }
}
