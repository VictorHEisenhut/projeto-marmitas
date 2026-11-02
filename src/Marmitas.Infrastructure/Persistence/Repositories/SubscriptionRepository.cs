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

    public Task<List<Plan>> GetActivePlansAsync()
    {
        return _context.Plans
            .Where(p => p.IsActive)
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
            .OrderByDescending(s => s.CreatedAt)
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

    public Task<List<Coupon>> GetAllCouponsAsync()
    {
        return _context.Coupons
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

