using Marmitas.Application.Common.Interfaces;
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

        var plan = new Plan
        {
            Name = request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            MealsPerWeek = request.MealsPerWeek,
            MonthlyPrice = request.MonthlyPrice,
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
        plan.MealsPerWeek = request.MealsPerWeek;
        plan.MonthlyPrice = request.MonthlyPrice;

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

public class SavePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealsPerWeek { get; set; }
    public decimal MonthlyPrice { get; set; }
}
