using Marmitas.Application.Orders.Dtos;
using Marmitas.Domain.Entities;

namespace Marmitas.Application.Common.Interfaces;

public interface IOrderPaymentSettlement
{
    Task<Payment> RecordChargeAsync(
        OrderDto order,
        Payment? existing,
        string method,
        PaymentGatewayResult result,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Payment?> RefreshAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> RefreshByExternalIdAsync(string externalPaymentId, CancellationToken cancellationToken = default);
    Task RefreshPendingAsync(CancellationToken cancellationToken = default);
}
