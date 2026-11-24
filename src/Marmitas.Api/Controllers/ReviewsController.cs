using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Orders;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IPaymentRepository _payments;
    private readonly ICreateOrderService _orders;
    private readonly IUserRepository _users;
    private readonly IRestaurantRepository _restaurants;

    public ReviewsController(
        IPaymentRepository payments,
        ICreateOrderService orders,
        IUserRepository users,
        IRestaurantRepository restaurants)
    {
        _payments = payments;
        _orders = orders;
        _users = users;
        _restaurants = restaurants;
    }

    [HttpGet("api/avaliacoes/clientes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCustomerReviews()
    {
        var list = await _payments.GetReviewsByTargetTypeAsync("Customer");
        return Ok(list);
    }

    [HttpGet("api/avaliacoes/destaques")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeaturedReviews([FromQuery] int limit = 3)
    {
        limit = Math.Clamp(limit, 1, 12);

        var list = await _payments.GetReviewsByTargetTypeAsync("Customer");
        var reviewCountByRestaurant = list
            .Where(r => r.RestaurantId != Guid.Empty)
            .GroupBy(r => r.RestaurantId)
            .ToDictionary(g => g.Key, g => g.Count());

        var withComments = list.Where(r => !string.IsNullOrWhiteSpace(r.Comment)).ToList();
        var candidates = withComments.Count > 0 ? withComments : list;

        var ordered = candidates
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r =>
                r.RestaurantId != Guid.Empty && reviewCountByRestaurant.TryGetValue(r.RestaurantId, out var count)
                    ? count
                    : 0)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

        var selected = new List<Review>();
        var seenRestaurants = new HashSet<Guid>();

        foreach (var rev in ordered)
        {
            if (selected.Count >= limit) break;
            if (rev.RestaurantId == Guid.Empty || seenRestaurants.Add(rev.RestaurantId))
            {
                selected.Add(rev);
            }
        }

        if (selected.Count < limit)
        {
            var selectedIds = selected.Select(r => r.Id).ToHashSet();
            foreach (var rev in ordered)
            {
                if (selected.Count >= limit) break;
                if (selectedIds.Add(rev.Id))
                {
                    selected.Add(rev);
                }
            }
        }

        return Ok(selected);
    }

    [HttpPost("api/avaliacoes/clientes")]
    [Authorize]
    public async Task<IActionResult> AddCustomerReview([FromBody] CreateTypedReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new Exception("A nota da avaliação deve ser entre 1 e 5.");

        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new Exception("O comentário da avaliação é obrigatório.");

        var userId = GetUserId();
        var user = userId != Guid.Empty ? await _users.GetByIdAsync(userId) : null;

        var restaurantName = request.RestaurantName?.Trim() ?? "Restaurante Parceiro";
        if (request.RestaurantId.HasValue && request.RestaurantId.Value != Guid.Empty)
        {
            var rest = await _restaurants.GetByIdAsync(request.RestaurantId.Value);
            if (rest != null) restaurantName = rest.Name;
        }

        var review = new Review
        {
            OrderId = request.OrderId ?? Guid.Empty,
            RestaurantId = request.RestaurantId ?? Guid.Empty,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            TargetType = "Customer",
            AuthorName = user?.Name ?? request.AuthorName ?? "Cliente Verificado",
            RestaurantName = restaurantName
        };

        await _payments.AddReviewAsync(review);
        await _payments.SaveChangesAsync();

        return StatusCode(201, review);
    }

    [HttpGet("api/avaliacoes/entregadores")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> GetCourierReviews()
    {
        var list = await _payments.GetReviewsByTargetTypeAsync("Courier");
        return Ok(list);
    }

    [HttpPost("api/avaliacoes/entregadores")]
    [Authorize(Roles = "driver,admin,super_admin")]
    public async Task<IActionResult> AddCourierReview([FromBody] CreateTypedReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new Exception("A nota da avaliação deve ser entre 1 e 5.");

        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new Exception("O comentário da avaliação do entregador é obrigatório.");

        var userId = GetUserId();
        var user = userId != Guid.Empty ? await _users.GetByIdAsync(userId) : null;

        var restaurantName = request.RestaurantName?.Trim() ?? "Geral / Coleta";
        if (request.RestaurantId.HasValue && request.RestaurantId.Value != Guid.Empty)
        {
            var rest = await _restaurants.GetByIdAsync(request.RestaurantId.Value);
            if (rest != null) restaurantName = rest.Name;
        }

        var review = new Review
        {
            OrderId = request.OrderId ?? Guid.Empty,
            RestaurantId = request.RestaurantId ?? Guid.Empty,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            TargetType = "Courier",
            AuthorName = user?.Name ?? request.AuthorName ?? "Entregador Parceiro",
            RestaurantName = restaurantName
        };

        await _payments.AddReviewAsync(review);
        await _payments.SaveChangesAsync();

        return StatusCode(201, review);
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

        var userId = GetUserId();
        if (userId == Guid.Empty) userId = order.CustomerId;
        var user = await _users.GetByIdAsync(userId);

        var review = new Review
        {
            OrderId = order.Id,
            RestaurantId = order.RestaurantId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment ?? string.Empty,
            TargetType = "Customer",
            AuthorName = user?.Name ?? order.CustomerName ?? "Cliente",
            RestaurantName = order.RestaurantName
        };

        await _payments.AddReviewAsync(review);
        await _payments.SaveChangesAsync();

        return StatusCode(201, review);
    }

    [HttpGet("api/restaurantes/{id}/avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRestaurantReviews(Guid id, [FromQuery] string? targetType = "Customer")
    {
        var list = await _payments.GetReviewsByRestaurantIdAsync(id);
        if (!string.Equals(targetType, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedTarget = string.IsNullOrWhiteSpace(targetType) ? "Customer" : targetType.Trim();
            list = list
                .Where(r => string.Equals(r.TargetType, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
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

        if (review.UserId != GetUserId() && !User.IsInRole("admin") && !User.IsInRole("super_admin"))
            return Forbid();

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

        if (review.UserId != GetUserId() && !User.IsInRole("admin") && !User.IsInRole("super_admin"))
            return Forbid();

        _payments.RemoveReview(review);
        await _payments.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var parsed) ? parsed : Guid.Empty;
    }
}
