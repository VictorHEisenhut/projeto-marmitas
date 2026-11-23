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

    public Task<List<Payment>> GetAllPaymentsAsync()
    {
        return _context.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task AddPaymentTransactionAsync(PaymentTransaction transaction)
    {
        await _context.PaymentTransactions.AddAsync(transaction);
    }

    public Task<List<PaymentTransaction>> GetPaymentTransactionsAsync(Guid paymentId)
    {
        return _context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.PaymentId == paymentId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddRefundAsync(Refund refund)
    {
        await _context.Refunds.AddAsync(refund);
    }

    public Task<List<Refund>> GetRefundsByOrderIdAsync(Guid orderId)
    {
        return _context.Refunds
            .AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.ProcessedAt)
            .ToListAsync();
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

    public async Task AddCommissionAsync(Commission commission)
    {
        await _context.Commissions.AddAsync(commission);
    }

    public Task<List<Commission>> GetAllCommissionsAsync()
    {
        return _context.Commissions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddFinancialTransactionAsync(FinancialTransaction transaction)
    {
        await _context.FinancialTransactions.AddAsync(transaction);
    }

    public Task<List<FinancialTransaction>> GetFinancialTransactionsAsync(Guid? targetId = null, string? targetType = null)
    {
        var query = _context.FinancialTransactions.AsNoTracking().AsQueryable();
        if (targetId.HasValue)
            query = query.Where(t => t.TargetId == targetId.Value || t.OrderId == targetId.Value);
        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(t => t.TargetType == targetType);

        return query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
    }

    public Task<List<AuditLog>> GetAuditLogsAsync(int limit = 200)
    {
        return _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public Task<AuditLog?> GetAuditLogByIdAsync(Guid id)
    {
        return _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, string path)
    {
        return _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.RequestPath == path && r.ExpiresAt > DateTime.UtcNow);
    }

    public async Task AddIdempotencyRecordAsync(IdempotencyRecord record)
    {
        await _context.IdempotencyRecords.AddAsync(record);
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
            .AsNoTracking()
            .Where(r => r.RestaurantId == restaurantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId)
    {
        var orderIds = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        return await _context.Reviews
            .AsNoTracking()
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
            .AsNoTracking()
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
