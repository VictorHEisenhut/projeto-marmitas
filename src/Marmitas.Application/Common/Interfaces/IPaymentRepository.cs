using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId);
    Task<Payment?> GetPaymentByIdAsync(Guid id);
    Task AddPaymentAsync(Payment payment);

    Task<List<Payout>> GetAllPayoutsAsync();
    Task<Payout?> GetPayoutByIdAsync(Guid id);
    Task AddPayoutAsync(Payout payout);

    Task<Review?> GetReviewByOrderIdAsync(Guid orderId);
    Task<Review?> GetReviewByIdAsync(Guid id);
    Task<List<Review>> GetReviewsByRestaurantIdAsync(Guid restaurantId);
    Task<List<Review>> GetReviewsByProductIdAsync(Guid productId);
    Task AddReviewAsync(Review review);
    void RemoveReview(Review review);

    Task SaveChangesAsync();
}

