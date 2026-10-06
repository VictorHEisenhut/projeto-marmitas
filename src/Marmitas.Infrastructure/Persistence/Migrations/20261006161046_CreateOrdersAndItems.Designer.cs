
using System;
using Marmitas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marmitas.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261006161046_CreateOrdersAndItems")]
    partial class CreateOrdersAndItems
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Marmitas.Domain.Entities.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("CustomerId")
                        .HasColumnType("uuid")
                        .HasColumnName("customer_id");

                    b.Property<Guid>("RestaurantId")
                        .HasColumnType("uuid")
                        .HasColumnName("restaurant_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)")
                        .HasColumnName("status");

                    b.Property<decimal>("TotalAmount")
                        .HasPrecision(10, 2)
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("total_amount");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_orders");

                    b.HasIndex("CustomerId")
                        .HasDatabaseName("ix_orders_customer_id");

                    b.HasIndex("RestaurantId")
                        .HasDatabaseName("ix_orders_restaurant_id");

                    b.ToTable("orders", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uuid")
                        .HasColumnName("product_id");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("product_name");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.Property<decimal>("TotalPrice")
                        .HasPrecision(10, 2)
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("total_price");

                    b.Property<decimal>("UnitPrice")
                        .HasPrecision(10, 2)
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("unit_price");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_order_items");

                    b.HasIndex("OrderId")
                        .HasDatabaseName("ix_order_items_order_id");

                    b.HasIndex("ProductId")
                        .HasDatabaseName("ix_order_items_product_id");

                    b.ToTable("order_items", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Permission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_permissions");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("ix_permissions_name");

                    b.ToTable("permissions", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000001"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Consultar pedidos",
                            Name = "orders.read",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000002"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Criar pedidos",
                            Name = "orders.create",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000003"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Atualizar status de pedidos",
                            Name = "orders.update",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000004"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Cancelar pedidos",
                            Name = "orders.cancel",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000005"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Consultar pagamentos",
                            Name = "payments.read",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000006"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Reembolsar pagamentos",
                            Name = "payments.refund",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000007"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Aprovar restaurantes",
                            Name = "restaurants.approve",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000008"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Aprovar entregadores",
                            Name = "drivers.approve",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000009"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Consultar financeiro",
                            Name = "financial.read",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0002-0000-0000-000000000010"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Executar repasses",
                            Name = "financial.payout",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        });
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CategoryId")
                        .HasColumnType("uuid")
                        .HasColumnName("category_id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("deleted_at");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("description");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)")
                        .HasColumnName("image_url");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean")
                        .HasColumnName("is_active");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("name");

                    b.Property<decimal>("Price")
                        .HasPrecision(10, 2)
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("price");

                    b.Property<Guid>("RestaurantId")
                        .HasColumnType("uuid")
                        .HasColumnName("restaurant_id");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_products");

                    b.HasIndex("CategoryId")
                        .HasDatabaseName("ix_products_category_id");

                    b.HasIndex("RestaurantId")
                        .HasDatabaseName("ix_products_restaurant_id");

                    b.ToTable("products", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.ProductCategory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("integer")
                        .HasColumnName("display_order");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<Guid>("RestaurantId")
                        .HasColumnType("uuid")
                        .HasColumnName("restaurant_id");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_product_categories");

                    b.HasIndex("RestaurantId")
                        .HasDatabaseName("ix_product_categories_restaurant_id");

                    b.ToTable("product_categories", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Restaurant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("deleted_at");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("description");

                    b.Property<string>("Document")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("document");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)")
                        .HasColumnName("image_url");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("name");

                    b.Property<Guid>("OwnerId")
                        .HasColumnType("uuid")
                        .HasColumnName("owner_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)")
                        .HasColumnName("status");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_restaurants");

                    b.HasIndex("OwnerId")
                        .HasDatabaseName("ix_restaurants_owner_id");

                    b.ToTable("restaurants", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Role", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("name");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_roles");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("ix_roles_name");

                    b.ToTable("roles", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Cliente final",
                            Name = "customer",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000002"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Dono do restaurante",
                            Name = "restaurant_owner",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000003"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Funcion�rio do restaurante",
                            Name = "restaurant_staff",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000004"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Entregador",
                            Name = "driver",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Opera��o da plataforma",
                            Name = "admin",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Configura��es globais e financeiro",
                            Name = "super_admin",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        });
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.RolePermission", b =>
                {
                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid")
                        .HasColumnName("role_id");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("uuid")
                        .HasColumnName("permission_id");

                    b.HasKey("RoleId", "PermissionId")
                        .HasName("pk_role_permissions");

                    b.HasIndex("PermissionId")
                        .HasDatabaseName("ix_role_permissions_permission_id");

                    b.ToTable("role_permissions", (string)null);

                    b.HasData(
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000002")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000004")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000002"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000002"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000003")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000003"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000003"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000003")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000004"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000003")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000004")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000005")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000006")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000007")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000008")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000005"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000009")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000001")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000002")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000003")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000004")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000005")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000006")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000007")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000008")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000009")
                        },
                        new
                        {
                            RoleId = new Guid("a1b2c3d4-0001-0000-0000-000000000006"),
                            PermissionId = new Guid("a1b2c3d4-0002-0000-0000-000000000010")
                        });
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("deleted_at");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)")
                        .HasColumnName("email");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("name");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("password_hash");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("status");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.UserRole", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid")
                        .HasColumnName("role_id");

                    b.HasKey("UserId", "RoleId")
                        .HasName("pk_user_roles");

                    b.HasIndex("RoleId")
                        .HasDatabaseName("ix_user_roles_role_id");

                    b.ToTable("user_roles", (string)null);
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Order", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.User", "Customer")
                        .WithMany()
                        .HasForeignKey("CustomerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_orders_users_customer_id");

                    b.HasOne("Marmitas.Domain.Entities.Restaurant", "Restaurant")
                        .WithMany()
                        .HasForeignKey("RestaurantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_orders_restaurants_restaurant_id");

                    b.Navigation("Customer");

                    b.Navigation("Restaurant");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.OrderItem", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.Order", "Order")
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_order_items_orders_order_id");

                    b.HasOne("Marmitas.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_order_items_products_product_id");

                    b.Navigation("Order");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Product", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.ProductCategory", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_products_product_categories_category_id");

                    b.HasOne("Marmitas.Domain.Entities.Restaurant", "Restaurant")
                        .WithMany("Products")
                        .HasForeignKey("RestaurantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_products_restaurants_restaurant_id");

                    b.Navigation("Category");

                    b.Navigation("Restaurant");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.ProductCategory", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.Restaurant", "Restaurant")
                        .WithMany("Categories")
                        .HasForeignKey("RestaurantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_product_categories_restaurants_restaurant_id");

                    b.Navigation("Restaurant");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Restaurant", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_restaurants_users_owner_id");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.RolePermission", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.Permission", "Permission")
                        .WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_role_permissions_permissions_permission_id");

                    b.HasOne("Marmitas.Domain.Entities.Role", "Role")
                        .WithMany("RolePermissions")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_role_permissions_roles_role_id");

                    b.Navigation("Permission");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.UserRole", b =>
                {
                    b.HasOne("Marmitas.Domain.Entities.Role", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_user_roles_roles_role_id");

                    b.HasOne("Marmitas.Domain.Entities.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_roles_users_user_id");

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Order", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.ProductCategory", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Restaurant", b =>
                {
                    b.Navigation("Categories");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.Role", b =>
                {
                    b.Navigation("RolePermissions");
                });

            modelBuilder.Entity("Marmitas.Domain.Entities.User", b =>
                {
                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
