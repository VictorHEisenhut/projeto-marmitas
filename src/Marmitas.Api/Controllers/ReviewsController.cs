using System.Security.Claims;
using Marmitas.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class ReviewsController : ControllerBase
{
    private static readonly List<ReviewDto> Reviews = new();
    private readonly ICreateOrderService _orders;

    public ReviewsController(ICreateOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("api/pedidos/{id}/avaliacao")]
    [AllowAnonymous]
    public IActionResult GetOrderReview(Guid id)
    {
        var review = Reviews.FirstOrDefault(r => r.OrderId == id);
        if (review == null)
            throw new Exception("Avaliação não encontrada para este pedido.");

        return Ok(review);
    }

    [HttpPost("api/pedidos/{id}/avaliacao")]
    [Authorize]
    public async Task<IActionResult> AddOrderReview(Guid id, [FromBody] SaveReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new Exception("A nota da avaliação deve ser entre 1 e 5.");

        var order = await _orders.GetByIdAsync(id);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : order.CustomerId;

        var review = new ReviewDto
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            UserId = userId,
            ProductIds = order.Items.Select(i => i.ProductId).ToList(),
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        Reviews.Add(review);
        return StatusCode(201, review);
    }

    [HttpGet("api/restaurantes/{id}/avaliacoes")]
    [AllowAnonymous]
    public IActionResult GetRestaurantReviews(Guid id)
    {
        return Ok(Reviews.Where(r => r.RestaurantId == id));
    }

    [HttpGet("api/marmitas/{id}/avaliacoes")]
    [AllowAnonymous]
    public IActionResult GetProductReviews(Guid id)
    {
        return Ok(Reviews.Where(r => r.ProductIds.Contains(id)));
    }

    [HttpPut("api/avaliacoes/{id}")]
    [Authorize]
    public IActionResult UpdateReview(Guid id, [FromBody] SaveReviewRequest request)
    {
        var review = Reviews.FirstOrDefault(r => r.Id == id);
        if (review == null)
            throw new Exception("Avaliação não encontrada.");

        review.Rating = request.Rating;
        review.Comment = request.Comment ?? string.Empty;
        return Ok(review);
    }

    [HttpDelete("api/avaliacoes/{id}")]
    [Authorize]
    public IActionResult DeleteReview(Guid id)
    {
        var review = Reviews.FirstOrDefault(r => r.Id == id);
        if (review == null)
            throw new Exception("Avaliação não encontrada.");

        Reviews.Remove(review);
        return NoContent();
    }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public List<Guid> ProductIds { get; set; } = new();
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SaveReviewRequest
{
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
}
