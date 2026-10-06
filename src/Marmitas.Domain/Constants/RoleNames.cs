namespace Marmitas.Domain.Constants;

public static class RoleNames
{
    public const string Customer = "customer";
    public const string RestaurantOwner = "restaurant_owner";
    public const string RestaurantStaff = "restaurant_staff";
    public const string Driver = "driver";
    public const string Admin = "admin";
    public const string SuperAdmin = "super_admin";
}

public static class PermissionNames
{
    public const string OrdersRead = "orders.read";
    public const string OrdersCreate = "orders.create";
    public const string OrdersUpdate = "orders.update";
    public const string OrdersCancel = "orders.cancel";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsRefund = "payments.refund";
    public const string RestaurantsApprove = "restaurants.approve";
    public const string DriversApprove = "drivers.approve";
    public const string FinancialRead = "financial.read";
    public const string FinancialPayout = "financial.payout";

    public static readonly string[] All =
    {
        OrdersRead, OrdersCreate, OrdersUpdate, OrdersCancel,
        PaymentsRead, PaymentsRefund,
        RestaurantsApprove, DriversApprove,
        FinancialRead, FinancialPayout
    };
}

