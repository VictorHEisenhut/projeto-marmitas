using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId)
    {
        return _context.Payments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public Task<List<Payout>> GetAllPayoutsAsync()
    {
        return _context.Payouts
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public Task<Payout?> GetPayoutByIdAsync(Guid id)
    {
        return _context.Payouts.FirstOrDefaultAsync(p => p.Id == id || p.TargetId == id);
    }

    public async Task AddPayoutAsync(Payout payout)
    {
        await _context.Payouts.AddAsync(payout);
    }

    public Task<Review?> GetReviewByOrderIdAsync(Guid orderId)
    {
        return _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    public Task<Review?> GetReviewByIdAsync(Guid id)
    {
        return _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task<List<Review>> GetReviewsByRestaurantIdAsync(Guid restaurantId)
    {
        return _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId)
    {
        var orderIds = await _context.OrderItems
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        return await _context.Reviews
            .Where(r => orderIds.Contains(r.OrderId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddReviewAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
    }

    public Task<List<Review>> GetReviewsByTargetTypeAsync(string targetType)
    {
        return _context.Reviews
            .Where(r => r.TargetType == targetType)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public void RemoveReview(Review review)
    {
        _context.Reviews.Remove(review);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}

