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

    public PlansController(ISubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _subscriptions.GetActivePlansAsync();
        return Ok(plans);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Create([FromBody] SavePlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do plano é obrigatório.");

        var mealsPerWeek = request.MealsPerWeek > 0 ? request.MealsPerWeek : 5;
        var mealsPerMonth = request.MealsPerMonth.HasValue && request.MealsPerMonth.Value > 0
            ? request.MealsPerMonth.Value
            : mealsPerWeek * 4;

        var plan = new Plan
        {
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
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePlanRequest request)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            plan.Name = request.Name.Trim();
        plan.Description = request.Description ?? string.Empty;
        plan.MealsPerWeek = request.MealsPerWeek > 0 ? request.MealsPerWeek : plan.MealsPerWeek;
        plan.MealsPerMonth = request.MealsPerMonth ?? (plan.MealsPerWeek * 4);
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
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _subscriptions.GetPlanByIdAsync(id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        plan.IsActive = false;
        await _subscriptions.SaveChangesAsync();
        return NoContent();
    }
}
