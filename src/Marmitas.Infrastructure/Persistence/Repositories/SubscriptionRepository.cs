using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Plan>> GetActivePlansAsync(Guid? restaurantId = null)
    {
        var query = _context.Plans
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            query = query.Where(p => p.RestaurantId == restaurantId.Value);
        }

        return query
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync();
    }

    public Task<Plan?> GetPlanByIdAsync(Guid id)
    {
        return _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddPlanAsync(Plan plan)
    {
        await _context.Plans.AddAsync(plan);
    }

    public Task<List<Subscription>> GetSubscriptionsByUserAsync(Guid userId)
    {
        return _context.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<List<Subscription>> GetAllSubscriptionsAsync()
    {
        return _context.Subscriptions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<List<Subscription>> GetDueForRenewalAsync(DateTime referenceUtc)
    {
        return _context.Subscriptions
            .Where(s => (s.Status == "Active" || s.Status == "GracePeriod") && s.NextBillingDate <= referenceUtc)
            .ToListAsync();
    }

    public Task<Subscription?> GetSubscriptionByIdAsync(Guid id)
    {
        return _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddSubscriptionAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
    }

    public Task<List<SubscriptionDelivery>> GetSubscriptionDeliveriesAsync(Guid subscriptionId)
    {
        return _context.SubscriptionDeliveries
            .AsNoTracking()
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderBy(d => d.ScheduledDate)
            .ToListAsync();
    }

    public Task<SubscriptionDelivery?> GetSubscriptionDeliveryByIdAsync(Guid id)
    {
        return _context.SubscriptionDeliveries.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddSubscriptionDeliveryAsync(SubscriptionDelivery delivery)
    {
        await _context.SubscriptionDeliveries.AddAsync(delivery);
    }

    public void RemoveSubscriptionDelivery(SubscriptionDelivery delivery)
    {
        _context.SubscriptionDeliveries.Remove(delivery);
    }

    public Task<List<Coupon>> GetAllCouponsAsync()
    {
        return _context.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public Task<Coupon?> GetCouponByIdAsync(Guid id)
    {
        return _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<Coupon?> GetCouponByCodeAsync(string code)
    {
        var normalized = code.Trim().ToUpper();
        return _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == normalized && c.IsActive);
    }

    public async Task AddCouponAsync(Coupon coupon)
    {
        await _context.Coupons.AddAsync(coupon);
    }

    public void RemoveCoupon(Coupon coupon)
    {
        _context.Coupons.Remove(coupon);
    }

    public Task<List<CouponUsage>> GetCouponUsagesAsync(Guid couponId)
    {
        return _context.CouponUsages
            .AsNoTracking()
            .Where(u => u.CouponId == couponId)
            .OrderByDescending(u => u.UsedAt)
            .ToListAsync();
    }

    public async Task AddCouponUsageAsync(CouponUsage usage)
    {
        await _context.CouponUsages.AddAsync(usage);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
