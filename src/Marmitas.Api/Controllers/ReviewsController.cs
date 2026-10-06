using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class ReviewsController : ControllerBase
{
    [HttpGet("api/pedidos/{id}/avaliacao")] public IActionResult GetOrderReview(Guid id) => StatusCode(501);
    [HttpPost("api/pedidos/{id}/avaliacao")] public IActionResult AddOrderReview(Guid id) => StatusCode(501);
    [HttpGet("api/restaurantes/{id}/avaliacoes")] public IActionResult GetRestaurantReviews(Guid id) => StatusCode(501);
    [HttpGet("api/marmitas/{id}/avaliacoes")] public IActionResult GetProductReviews(Guid id) => StatusCode(501);
    [HttpPut("api/avaliacoes/{id}")] public IActionResult UpdateReview(Guid id) => StatusCode(501);
    [HttpDelete("api/avaliacoes/{id}")] public IActionResult DeleteReview(Guid id) => StatusCode(501);
}
