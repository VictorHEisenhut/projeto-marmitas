namespace Marmitas.Application.Platform.Dtos;

public class SaveAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsMain { get; set; }
}

public class AddFavoriteRequest
{
    public Guid? RestaurantId { get; set; }
    public Guid? ProductId { get; set; }
}

public class SaveCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
}

public class RegisterCourierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Cpf { get; set; }
    public string? Cnh { get; set; }
    public string? Phone { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleModel { get; set; }
    public string? VehiclePlate { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingNumber { get; set; }
    public string? BillingComplement { get; set; }
    public string? BillingNeighborhood { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZipCode { get; set; }
}

public class UpdateCourierStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SaveCourierDocumentRequest
{
    public string Type { get; set; } = "CNH";
    public string Number { get; set; } = string.Empty;
}

public class SaveCourierVehicleRequest
{
    public string Type { get; set; } = "Moto";
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}

public class SaveCourierBillingAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class CourierBillingAddressDto : SaveCourierBillingAddressRequest
{
    public bool IsComplete { get; set; }
}

public class UpdateLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? DeliveryId { get; set; }
    public Guid? OrderId { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public string Status { get; set; } = "Delivered";
    public string? ConfirmationPin { get; set; }
}

public class UpdateCourierAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}

public class CreateFinancialAdjustmentRequest
{
    public string TargetType { get; set; } = "Restaurant";
    public Guid? TargetId { get; set; }
    public string EntryType { get; set; } = "Credit";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class NotificationPreferencesDto
{
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
}

public class ProcessPaymentRequest
{
    public string Method { get; set; } = "PIX";
    public string? Gateway { get; set; }
    public string? CardToken { get; set; }
}

public class RefundPaymentRequest
{
    public string? Reason { get; set; }
}

public class PaymentWebhookRequest
{
    public Guid? PaymentId { get; set; }
    public string? Status { get; set; }
    public string? Gateway { get; set; }
    public string? ExternalTransactionId { get; set; }
}

public class SavePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealsPerWeek { get; set; }
    public int? MealsPerMonth { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string? Frequency { get; set; }
    public string? AllowedDays { get; set; }
    public string? DeliveryWindow { get; set; }
    public decimal? DeliveryFee { get; set; }
    public string? CancellationPolicy { get; set; }
}

public class AddRestaurantStaffRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "restaurant_staff";
}

public class SaveReviewRequest
{
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
}

public class CreateTypedReviewRequest
{
    public Guid? OrderId { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; } = 5;
    public string Comment { get; set; } = string.Empty;
}

public class CreateSubscriptionRequest
{
    public Guid PlanId { get; set; }
    public Guid? PreferredRestaurantId { get; set; }
    public Guid? DefaultAddressId { get; set; }
    public string? PaymentMethod { get; set; } = "PIX";
    public string? CardToken { get; set; }
}

public class ChangeSubscriptionPlanRequest
{
    public Guid NewPlanId { get; set; }
}

public class UpdateSubscriptionSettingsRequest
{
    public Guid? DefaultAddressId { get; set; }
    public Guid? PreferredRestaurantId { get; set; }
}

public class ScheduleSubscriptionDeliveryRequest
{
    public Guid? RestaurantId { get; set; }
    public Guid? AddressId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string DeliveryWindow { get; set; } = "11:30 - 13:30";
    public int MealsCount { get; set; } = 1;
}
