using Marmitas.Domain.Exceptions;

namespace Marmitas.Domain.Entities;

public class Delivery : Entity
{
    public Guid OrderId { get; set; }
    public Guid? CourierId { get; set; }
    public string Status { get; set; } = "ReadyForPickup";
    public double? DistanceKm { get; set; }
    public decimal DeliveryFee { get; set; } = 8.50m;
    public string? ConfirmationPin { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public void AssignCourier(Guid courierId)
    {
        if (CourierId.HasValue && CourierId.Value != Guid.Empty && CourierId.Value != courierId && Status != "ReadyForPickup")
            throw DomainException.Conflict("Esta entrega já foi aceita por outro entregador.");

        CourierId = courierId;
        Status = "DriverAssigned";
        AssignedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(string nextStatus, string? providedPin = null, bool isAdminOverride = false)
    {
        if (nextStatus == "Delivered" && !string.IsNullOrWhiteSpace(ConfirmationPin))
        {
            if (!isAdminOverride && string.IsNullOrWhiteSpace(providedPin))
                throw DomainException.Validation("Informe o código PIN de 4 dígitos do cliente para confirmar a entrega.");

            if (!string.IsNullOrWhiteSpace(providedPin) && ConfirmationPin.Trim() != providedPin.Trim())
                throw DomainException.Validation("Código de confirmação de entrega inválido.");
        }

        if (nextStatus == "PickedUp")
            PickedUpAt = DateTime.UtcNow;
        else if (nextStatus == "Delivered")
            DeliveredAt = DateTime.UtcNow;

        Status = nextStatus;
    }
}
