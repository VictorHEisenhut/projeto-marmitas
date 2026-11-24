using System.Text.Json;
using Marmitas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marmitas.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private bool _savingAudit;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<RestaurantUser> RestaurantUsers => Set<RestaurantUser>();
    public DbSet<RestaurantHour> RestaurantHours => Set<RestaurantHour>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionItem> SubscriptionItems => Set<SubscriptionItem>();
    public DbSet<SubscriptionDelivery> SubscriptionDeliveries => Set<SubscriptionDelivery>();

    public DbSet<RestaurantIntegration> RestaurantIntegrations => Set<RestaurantIntegration>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<CourierDocument> CourierDocuments => Set<CourierDocument>();
    public DbSet<CourierVehicle> CourierVehicles => Set<CourierVehicle>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<User>().Property(u => u.Status).HasConversion<string>();
        modelBuilder.Entity<Restaurant>().Property(r => r.Status).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<string>();

        var customerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<Role>().HasData(
            new Role("customer", "Cliente da plataforma") { Id = customerRoleId }
        );

        modelBuilder.Entity<ProductCategory>()
            .HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .IsRequired(false);

        modelBuilder.Entity<RestaurantIntegration>()
            .HasIndex(i => new { i.RestaurantId, i.Provider })
            .IsUnique();

        modelBuilder.Entity<RestaurantIntegration>()
            .HasOne(i => i.Restaurant)
            .WithMany()
            .HasForeignKey(i => i.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductCategory>().HasData(
            new ProductCategory(null, "Tradicionais", 1) { Id = Guid.Parse("00000000-0000-0000-0002-000000000001") },
            new ProductCategory(null, "Fitness & Low Carb", 2) { Id = Guid.Parse("00000000-0000-0000-0002-000000000002") },
            new ProductCategory(null, "Executiva", 3) { Id = Guid.Parse("00000000-0000-0000-0002-000000000003") },
            new ProductCategory(null, "Vegetariana & Vegana", 4) { Id = Guid.Parse("00000000-0000-0000-0002-000000000004") },
            new ProductCategory(null, "Bebidas & Sobremesas", 5) { Id = Guid.Parse("00000000-0000-0000-0002-000000000005") }
        );

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        CaptureAuditLogs();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private static readonly HashSet<string> AuditedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Order),
        nameof(Payment),
        nameof(Refund),
        nameof(Payout),
        nameof(Restaurant),
        nameof(Product),
        nameof(Plan),
        nameof(Subscription),
        nameof(Courier),
        nameof(Coupon)
    };

    private void CaptureAuditLogs()
    {
        if (_savingAudit) return;

        try
        {
            _savingAudit = true;
            var now = DateTime.UtcNow;
            var pendingAudits = new List<AuditLog>();

            foreach (var entry in ChangeTracker.Entries<Entity>().ToList())
            {
                if (entry.Entity is AuditLog or DriverLocation or Notification or RefreshToken or IdempotencyRecord)
                    continue;

                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;

                var typeName = entry.Entity.GetType().Name;
                if (!AuditedEntities.Contains(typeName))
                    continue;

                var action = entry.State switch
                {
                    EntityState.Added => $"{typeName.ToUpperInvariant()}_CREATED",
                    EntityState.Modified => $"{typeName.ToUpperInvariant()}_UPDATED",
                    EntityState.Deleted => $"{typeName.ToUpperInvariant()}_DELETED",
                    _ => $"{typeName.ToUpperInvariant()}_CHANGED"
                };

                var newValues = new Dictionary<string, object?>();
                var oldValues = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.Name is "PasswordHash" or "MfaSecret" or "AddonsJson" or "AccessToken")
                        continue;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else if (entry.State == EntityState.Modified && prop.IsModified)
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                pendingAudits.Add(new AuditLog
                {
                    Action = action,
                    EntityName = typeName,
                    EntityId = entry.Entity.Id.ToString(),
                    OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (pendingAudits.Count > 0)
            {
                AuditLogs.AddRange(pendingAudits);
            }
        }
        finally
        {
            _savingAudit = false;
        }
    }
}
