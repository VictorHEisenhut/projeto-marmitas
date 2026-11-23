using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface ISubscriptionRepository
{
    Task<List<Plan>> GetActivePlansAsync();
    Task<Plan?> GetPlanByIdAsync(Guid id);
    Task AddPlanAsync(Plan plan);

    Task<List<Subscription>> GetSubscriptionsByUserAsync(Guid userId);
    Task<List<Subscription>> GetAllSubscriptionsAsync();
    Task<List<Subscription>> GetDueForRenewalAsync(DateTime referenceUtc);
    Task<Subscription?> GetSubscriptionByIdAsync(Guid id);
    Task AddSubscriptionAsync(Subscription subscription);

    Task<List<SubscriptionDelivery>> GetSubscriptionDeliveriesAsync(Guid subscriptionId);
    Task<SubscriptionDelivery?> GetSubscriptionDeliveryByIdAsync(Guid id);
    Task AddSubscriptionDeliveryAsync(SubscriptionDelivery delivery);
    void RemoveSubscriptionDelivery(SubscriptionDelivery delivery);

    Task<List<Coupon>> GetAllCouponsAsync();
    Task<Coupon?> GetCouponByIdAsync(Guid id);
    Task<Coupon?> GetCouponByCodeAsync(string code);
    Task AddCouponAsync(Coupon coupon);
    void RemoveCoupon(Coupon coupon);
    Task<List<CouponUsage>> GetCouponUsagesAsync(Guid couponId);
    Task AddCouponUsageAsync(CouponUsage usage);

    Task SaveChangesAsync();
}
