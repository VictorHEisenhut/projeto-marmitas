using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IPaymentRepository _payments;
    private readonly ICreateOrderService _orders;

    public ReviewsController(IPaymentRepository payments, ICreateOrderService orders)
    {
        _payments = payments;
        _orders = orders;
    }

    [HttpGet("api/pedidos/{id}/avaliacao")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderReview(Guid id)
    {
        var review = await _payments.GetReviewByOrderIdAsync(id);
        if (review == null)
            throw new Exception("Avaliação não encontrada para este pedido.");

        return Ok(review);
    }

    [HttpPost("api/pedidos/{id}/avaliacao")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> AddOrderReview(Guid id, [FromBody] SaveReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new Exception("A nota da avaliação deve ser entre 1 e 5.");

        var order = await _orders.GetByIdAsync(id);
        var existing = await _payments.GetReviewByOrderIdAsync(id);
        if (existing != null)
            throw new Exception("Este pedido já possui uma avaliação.");

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.TryParse(sub, out var parsed) ? parsed : order.CustomerId;

        var review = new Review
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty
        };

        await _payments.AddReviewAsync(review);
        await _payments.SaveChangesAsync();

        return StatusCode(201, review);
    }

    [HttpGet("api/restaurantes/{id}/avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRestaurantReviews(Guid id)
    {
        var list = await _payments.GetReviewsByRestaurantIdAsync(id);
        return Ok(list);
    }

    [HttpGet("api/marmitas/{id}/avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(Guid id)
    {
        var list = await _payments.GetReviewsByProductIdAsync(id);
        return Ok(list);
    }

    [HttpPut("api/avaliacoes/{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] SaveReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new Exception("A nota da avaliação deve ser entre 1 e 5.");

        var review = await _payments.GetReviewByIdAsync(id);
        if (review == null)
            throw new Exception("Avaliação não encontrada.");

        review.Rating = request.Rating;
        review.Comment = request.Comment ?? string.Empty;

        await _payments.SaveChangesAsync();
        return Ok(review);
    }

    [HttpDelete("api/avaliacoes/{id}")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var review = await _payments.GetReviewByIdAsync(id);
        if (review == null)
            throw new Exception("Avaliação não encontrada.");

        _payments.RemoveReview(review);
        await _payments.SaveChangesAsync();
        return NoContent();
    }
}

public class SaveReviewRequest
{
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
}
