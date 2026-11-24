using Marmitas.Domain.Exceptions;

namespace Marmitas.Domain.Entities;

public class Subscription : Entity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; }
    public DateTime NextBillingDate { get; set; }

    public int TotalMealsInCycle { get; set; } = 20;
    public int UsedMealsInCycle { get; set; }
    public Guid? PreferredRestaurantId { get; set; }
    public Guid? DefaultAddressId { get; set; }
    public int FailedPaymentAttempts { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public int GetRemainingMeals() => Math.Max(0, TotalMealsInCycle - UsedMealsInCycle);

    public void Pause()
    {
        if (Status == "Cancelled" || Status == "Expired")
            throw DomainException.Conflict("Não é possível pausar uma assinatura cancelada ou expirada.");

        Status = "Paused";
    }

    public void Reactivate()
    {
        if (Status == "Cancelled")
            throw DomainException.Conflict("Assinatura cancelada não pode ser reativada diretamente.");

        Status = "Active";
        GracePeriodEndsAt = null;
        FailedPaymentAttempts = 0;
    }

    public void Renew(int mealsInCycle)
    {
        Status = "Active";
        StartDate = DateTime.UtcNow;
        NextBillingDate = DateTime.UtcNow.AddMonths(1);
        TotalMealsInCycle = mealsInCycle > 0 ? mealsInCycle : TotalMealsInCycle;
        UsedMealsInCycle = 0;
        FailedPaymentAttempts = 0;
        GracePeriodEndsAt = null;
    }

    public void Cancel()
    {
        Status = "Cancelled";
        CancelledAt = DateTime.UtcNow;
    }

    public void RegisterPaymentFailure()
    {
        FailedPaymentAttempts++;
        Status = FailedPaymentAttempts >= 3 ? "Suspended" : "GracePeriod";
        GracePeriodEndsAt ??= DateTime.UtcNow.AddDays(5);
    }

    public decimal ChangePlan(Plan newPlan)
    {
        if (RestaurantId != Guid.Empty && newPlan.RestaurantId != Guid.Empty && RestaurantId != newPlan.RestaurantId)
            throw DomainException.Validation("Só é possível alterar para outro plano do mesmo restaurante.");

        var remainingDays = Math.Max(0, (NextBillingDate - DateTime.UtcNow).TotalDays);
        var cycleRatio = (decimal)Math.Min(1.0, remainingDays / 30.0);
        var currentCredit = Math.Round(MonthlyPrice * cycleRatio, 2);
        var newProRataCharge = Math.Round(newPlan.MonthlyPrice * cycleRatio, 2);
        var difference = Math.Round(newProRataCharge - currentCredit, 2);

        PlanId = newPlan.Id;
        PlanName = newPlan.Name;
        if (newPlan.RestaurantId != Guid.Empty)
        {
            RestaurantId = newPlan.RestaurantId;
            PreferredRestaurantId = newPlan.RestaurantId;
        }
        if (!string.IsNullOrWhiteSpace(newPlan.RestaurantName))
            RestaurantName = newPlan.RestaurantName;

        MonthlyPrice = newPlan.MonthlyPrice;
        TotalMealsInCycle = newPlan.MealsPerMonth > 0 ? newPlan.MealsPerMonth : newPlan.MealsPerWeek * 4;
        return difference;
    }
}
