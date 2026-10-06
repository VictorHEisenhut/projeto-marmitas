using Marmitas.Domain.Constants;

namespace Marmitas.Infrastructure.Persistence.Seed;

public static class RbacSeed
{
    public static readonly DateTime SeedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid CustomerRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    public static readonly Guid RestaurantOwnerRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000002");
    public static readonly Guid RestaurantStaffRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000003");
    public static readonly Guid DriverRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000004");
    public static readonly Guid AdminRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000005");
    public static readonly Guid SuperAdminRoleId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000006");

    public static readonly Guid OrdersReadId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000001");
    public static readonly Guid OrdersCreateId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002");
    public static readonly Guid OrdersUpdateId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000003");
    public static readonly Guid OrdersCancelId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000004");
    public static readonly Guid PaymentsReadId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000005");
    public static readonly Guid PaymentsRefundId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000006");
    public static readonly Guid RestaurantsApproveId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000007");
    public static readonly Guid DriversApproveId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000008");
    public static readonly Guid FinancialReadId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000009");
    public static readonly Guid FinancialPayoutId = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000010");

    public static object[] Roles()
    {
        return new object[]
        {
            NewRole(CustomerRoleId, RoleNames.Customer, "Cliente final"),
            NewRole(RestaurantOwnerRoleId, RoleNames.RestaurantOwner, "Dono do restaurante"),
            NewRole(RestaurantStaffRoleId, RoleNames.RestaurantStaff, "Funcionário do restaurante"),
            NewRole(DriverRoleId, RoleNames.Driver, "Entregador"),
            NewRole(AdminRoleId, RoleNames.Admin, "Operação da plataforma"),
            NewRole(SuperAdminRoleId, RoleNames.SuperAdmin, "Configurações globais e financeiro")
        };
    }

    public static object[] Permissions()
    {
        return new object[]
        {
            NewPermission(OrdersReadId, PermissionNames.OrdersRead, "Consultar pedidos"),
            NewPermission(OrdersCreateId, PermissionNames.OrdersCreate, "Criar pedidos"),
            NewPermission(OrdersUpdateId, PermissionNames.OrdersUpdate, "Atualizar status de pedidos"),
            NewPermission(OrdersCancelId, PermissionNames.OrdersCancel, "Cancelar pedidos"),
            NewPermission(PaymentsReadId, PermissionNames.PaymentsRead, "Consultar pagamentos"),
            NewPermission(PaymentsRefundId, PermissionNames.PaymentsRefund, "Reembolsar pagamentos"),
            NewPermission(RestaurantsApproveId, PermissionNames.RestaurantsApprove, "Aprovar restaurantes"),
            NewPermission(DriversApproveId, PermissionNames.DriversApprove, "Aprovar entregadores"),
            NewPermission(FinancialReadId, PermissionNames.FinancialRead, "Consultar financeiro"),
            NewPermission(FinancialPayoutId, PermissionNames.FinancialPayout, "Executar repasses")
        };
    }

    public static object[] RolePermissions()
    {
        var list = new List<object>();

        AddLinks(list, CustomerRoleId, OrdersReadId, OrdersCreateId, OrdersCancelId);

        AddLinks(list, RestaurantOwnerRoleId, OrdersReadId, OrdersUpdateId);
        AddLinks(list, RestaurantStaffRoleId, OrdersReadId, OrdersUpdateId);

        AddLinks(list, DriverRoleId, OrdersReadId);

        AddLinks(list, AdminRoleId,
            OrdersReadId, OrdersUpdateId, OrdersCancelId,
            PaymentsReadId, PaymentsRefundId,
            RestaurantsApproveId, DriversApproveId,
            FinancialReadId);

        AddLinks(list, SuperAdminRoleId,
            OrdersReadId, OrdersCreateId, OrdersUpdateId, OrdersCancelId,
            PaymentsReadId, PaymentsRefundId,
            RestaurantsApproveId, DriversApproveId,
            FinancialReadId, FinancialPayoutId);

        return list.ToArray();
    }

    private static object NewRole(Guid id, string name, string description)
    {
        return new { Id = id, Name = name, Description = description, CreatedAt = SeedDate, UpdatedAt = SeedDate };
    }

    private static object NewPermission(Guid id, string name, string description)
    {
        return new { Id = id, Name = name, Description = description, CreatedAt = SeedDate, UpdatedAt = SeedDate };
    }

    private static void AddLinks(List<object> list, Guid roleId, params Guid[] permissionIds)
    {
        foreach (var permissionId in permissionIds)
            list.Add(new { RoleId = roleId, PermissionId = permissionId });
    }
}

