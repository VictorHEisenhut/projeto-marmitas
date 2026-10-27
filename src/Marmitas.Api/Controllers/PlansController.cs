using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/planos")]
[ApiController]
public class PlansController : ControllerBase
{
    public static readonly List<PlanDto> Plans = new()
    {
        new PlanDto
        {
            Id = Guid.NewGuid(),
            Name = "Plano Semanal 5 Marmitas",
            Description = "Receba 1 marmita por dia útil da semana",
            MealsPerWeek = 5,
            MonthlyPrice = 399.90m,
            IsActive = true
        }
    };

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        return Ok(Plans.Where(p => p.IsActive));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetById(Guid id)
    {
        var plan = Plans.FirstOrDefault(p => p.Id == id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        return Ok(plan);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Create([FromBody] SavePlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("O nome do plano é obrigatório.");

        var plan = new PlanDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            MealsPerWeek = request.MealsPerWeek,
            MonthlyPrice = request.MonthlyPrice,
            IsActive = true
        };

        Plans.Add(plan);
        return StatusCode(201, plan);
    }

    [HttpPut("{id}")]
    [Authorize]
    public IActionResult Update(Guid id, [FromBody] SavePlanRequest request)
    {
        var plan = Plans.FirstOrDefault(p => p.Id == id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            plan.Name = request.Name.Trim();
        plan.Description = request.Description ?? string.Empty;
        plan.MealsPerWeek = request.MealsPerWeek;
        plan.MonthlyPrice = request.MonthlyPrice;

        return Ok(plan);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public IActionResult Delete(Guid id)
    {
        var plan = Plans.FirstOrDefault(p => p.Id == id);
        if (plan == null)
            throw new Exception("Plano não encontrado.");

        plan.IsActive = false;
        return NoContent();
    }
}

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealsPerWeek { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; }
}

public class SavePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealsPerWeek { get; set; }
    public decimal MonthlyPrice { get; set; }
}
