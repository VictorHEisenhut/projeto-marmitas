using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId);
    Task<Payment?> GetPaymentByIdAsync(Guid id);
    Task<List<Payment>> GetAllPaymentsAsync();
    Task AddPaymentAsync(Payment payment);

    Task AddPaymentTransactionAsync(PaymentTransaction transaction);
    Task<List<PaymentTransaction>> GetPaymentTransactionsAsync(Guid paymentId);

    Task AddRefundAsync(Refund refund);
    Task<List<Refund>> GetRefundsByOrderIdAsync(Guid orderId);

    Task<List<Payout>> GetAllPayoutsAsync();
    Task<Payout?> GetPayoutByIdAsync(Guid id);
    Task AddPayoutAsync(Payout payout);

    Task AddCommissionAsync(Commission commission);
    Task<List<Commission>> GetAllCommissionsAsync();

    Task AddFinancialTransactionAsync(FinancialTransaction transaction);
    Task<List<FinancialTransaction>> GetFinancialTransactionsAsync(Guid? targetId = null, string? targetType = null);

    Task AddAuditLogAsync(AuditLog log);
    Task<List<AuditLog>> GetAuditLogsAsync(int limit = 200);
    Task<AuditLog?> GetAuditLogByIdAsync(Guid id);

    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, string path);
    Task AddIdempotencyRecordAsync(IdempotencyRecord record);

    Task<Review?> GetReviewByOrderIdAsync(Guid orderId);
    Task<Review?> GetReviewByIdAsync(Guid id);
    Task<List<Review>> GetReviewsByRestaurantIdAsync(Guid restaurantId);
    Task<List<Review>> GetReviewsByProductIdAsync(Guid productId);
    Task<List<Review>> GetReviewsByTargetTypeAsync(string targetType);
    Task AddReviewAsync(Review review);
    void RemoveReview(Review review);

    Task SaveChangesAsync();
}
