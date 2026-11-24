using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Application.Platform.Dtos;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/planos")]
[ApiController]
public class PlansController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IRestaurantRepository _restaurants;

    public PlansController(ISubscriptionRepository subscriptions, IRestaurantRepository restaurants)
    {
        _subscriptions = subscriptions;
        _restaurants = restaurants;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] Guid? restaurantId = null)
    {
        var plans = await _subscriptions.GetActivePlansAsync(restaurantId);
        if (plans.Any(p => string.IsNullOrWhiteSpace(p.RestaurantName) && p.RestaurantId != Guid.Empty))
        {
            var restaurants = await _restaurants.GetAllAsync();
            var namesById = restaurants.ToDictionary(r => r.Id, r => r.Name);
            foreach (var p in plans)
            {
                if (string.IsNullOrWhiteSpace(p.RestaurantName) && namesById.TryGetValue(p.RestaurantId, out var name))
                {
                    p.RestaurantName = name;
                }
            }
        }
        return Ok(plans);
    }

    [HttpGet("/api/restaurantes/{restaurantId:guid}/planos")]
    [AllowAnonymous]
    public Task<IActionResult> GetByRestaurant(Guid restaurantId) => GetAll(restaurantId);

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        if (string.IsNullOrWhiteSpace(plan.RestaurantName) && plan.RestaurantId != Guid.Empty)
        {
            var restaurant = await _restaurants.GetByIdAsync(plan.RestaurantId);
            if (restaurant != null)
                plan.RestaurantName = restaurant.Name;
        }

        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> Create([FromBody] SavePlanRequest request)
    {
        if (request.RestaurantId == Guid.Empty)
            throw new Exception("O restaurante do plano é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do plano é obrigatório.");
        if (request.MonthlyPrice <= 0)
            throw new Exception("O valor mensal do plano deve ser maior que zero.");

        var restaurant = await _restaurants.GetByIdAsync(request.RestaurantId);
        if (restaurant == null)
            throw new Exception("Restaurante informado não encontrado.");

        if (!IsAdmin() && !await _restaurants.IsUserAuthorizedAsync(request.RestaurantId, GetUserId()))
            return Forbid();

        var mealsPerWeek = request.MealsPerWeek > 0 ? request.MealsPerWeek : 5;
        var mealsPerMonth = request.MealsPerMonth.HasValue && request.MealsPerMonth.Value > 0
            ? request.MealsPerMonth.Value
            : mealsPerWeek * 4;

        var plan = new Plan
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            Name = request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            MealsPerWeek = mealsPerWeek,
            MealsPerMonth = mealsPerMonth,
            MonthlyPrice = request.MonthlyPrice,
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? "Monthly" : request.Frequency.Trim(),
            AllowedDays = string.IsNullOrWhiteSpace(request.AllowedDays) ? "Segunda a Sexta" : request.AllowedDays.Trim(),
            DeliveryWindow = string.IsNullOrWhiteSpace(request.DeliveryWindow) ? "11:00 às 14:30" : request.DeliveryWindow.Trim(),
            DeliveryFee = request.DeliveryFee ?? 0m,
            CancellationPolicy = string.IsNullOrWhiteSpace(request.CancellationPolicy)
                ? "Cancelamento livre até o fim do ciclo vigente."
                : request.CancellationPolicy.Trim(),
            IsActive = true
        };

        await _subscriptions.AddPlanAsync(plan);
        await _subscriptions.SaveChangesAsync();

        return StatusCode(201, plan);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePlanRequest request)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        if (!IsAdmin() && !await _restaurants.IsUserAuthorizedAsync(plan.RestaurantId, GetUserId()))
            return Forbid();

        if (request.RestaurantId != Guid.Empty && request.RestaurantId != plan.RestaurantId)
        {
            if (!IsAdmin())
                return Forbid();

            var newRestaurant = await _restaurants.GetByIdAsync(request.RestaurantId);
            if (newRestaurant == null)
                throw new Exception("Restaurante informado não encontrado.");

            plan.RestaurantId = newRestaurant.Id;
            plan.RestaurantName = newRestaurant.Name;
        }
        else if (string.IsNullOrWhiteSpace(plan.RestaurantName) && plan.RestaurantId != Guid.Empty)
        {
            var restaurant = await _restaurants.GetByIdAsync(plan.RestaurantId);
            if (restaurant != null)
                plan.RestaurantName = restaurant.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            plan.Name = request.Name.Trim();
        plan.Description = request.Description ?? string.Empty;
        plan.MealsPerWeek = request.MealsPerWeek > 0 ? request.MealsPerWeek : plan.MealsPerWeek;
        plan.MealsPerMonth = request.MealsPerMonth ?? (plan.MealsPerWeek * 4);
        if (request.MonthlyPrice > 0)
            plan.MonthlyPrice = request.MonthlyPrice;

        if (!string.IsNullOrWhiteSpace(request.Frequency))
            plan.Frequency = request.Frequency.Trim();
        if (!string.IsNullOrWhiteSpace(request.AllowedDays))
            plan.AllowedDays = request.AllowedDays.Trim();
        if (!string.IsNullOrWhiteSpace(request.DeliveryWindow))
            plan.DeliveryWindow = request.DeliveryWindow.Trim();
        if (request.DeliveryFee.HasValue)
            plan.DeliveryFee = request.DeliveryFee.Value;
        if (!string.IsNullOrWhiteSpace(request.CancellationPolicy))
            plan.CancellationPolicy = request.CancellationPolicy.Trim();

        await _subscriptions.SaveChangesAsync();
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "restaurant_owner,restaurant_staff,admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        if (!IsAdmin() && !await _restaurants.IsUserAuthorizedAsync(plan.RestaurantId, GetUserId()))
            return Forbid();

        plan.IsActive = false;
        await _subscriptions.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("admin") || User.IsInRole("super_admin");
}
