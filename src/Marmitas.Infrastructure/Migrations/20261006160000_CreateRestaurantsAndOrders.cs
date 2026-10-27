using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    public partial class CreateRestaurantsAndOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS restaurants (
                    id uuid NOT NULL,
                    name character varying(150) NOT NULL,
                    description character varying(500) NOT NULL,
                    document character varying(20) NOT NULL,
                    image_url character varying(300),
                    status character varying(30) NOT NULL,
                    owner_id uuid NOT NULL,
                    deleted_at timestamp with time zone,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_restaurants PRIMARY KEY (id),
                    CONSTRAINT fk_restaurants_users_owner_id FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS orders (
                    id uuid NOT NULL,
                    customer_id uuid NOT NULL,
                    restaurant_id uuid NOT NULL,
                    total_amount numeric(10,2) NOT NULL,
                    status character varying(30) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_orders PRIMARY KEY (id),
                    CONSTRAINT fk_orders_restaurants_restaurant_id FOREIGN KEY (restaurant_id) REFERENCES restaurants (id) ON DELETE RESTRICT,
                    CONSTRAINT fk_orders_users_customer_id FOREIGN KEY (customer_id) REFERENCES users (id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS product_categories (
                    id uuid NOT NULL,
                    restaurant_id uuid NOT NULL,
                    name character varying(100) NOT NULL,
                    display_order integer NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_product_categories PRIMARY KEY (id),
                    CONSTRAINT fk_product_categories_restaurants_restaurant_id FOREIGN KEY (restaurant_id) REFERENCES restaurants (id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS products (
                    id uuid NOT NULL,
                    restaurant_id uuid NOT NULL,
                    category_id uuid NOT NULL,
                    name character varying(150) NOT NULL,
                    description character varying(500) NOT NULL,
                    price numeric(10,2) NOT NULL,
                    is_active boolean NOT NULL,
                    image_url character varying(300),
                    deleted_at timestamp with time zone,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_products PRIMARY KEY (id),
                    CONSTRAINT fk_products_product_categories_category_id FOREIGN KEY (category_id) REFERENCES product_categories (id) ON DELETE RESTRICT,
                    CONSTRAINT fk_products_restaurants_restaurant_id FOREIGN KEY (restaurant_id) REFERENCES restaurants (id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS order_items (
                    id uuid NOT NULL,
                    order_id uuid NOT NULL,
                    product_id uuid NOT NULL,
                    product_name character varying(150) NOT NULL,
                    unit_price numeric(10,2) NOT NULL,
                    quantity integer NOT NULL,
                    total_price numeric(10,2) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_order_items PRIMARY KEY (id),
                    CONSTRAINT fk_order_items_orders_order_id FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
                    CONSTRAINT fk_order_items_products_product_id FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_order_items_order_id ON order_items (order_id);
                CREATE INDEX IF NOT EXISTS ix_order_items_product_id ON order_items (product_id);
                CREATE INDEX IF NOT EXISTS ix_orders_customer_id ON orders (customer_id);
                CREATE INDEX IF NOT EXISTS ix_orders_restaurant_id ON orders (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_product_categories_restaurant_id ON product_categories (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_products_category_id ON products (category_id);
                CREATE INDEX IF NOT EXISTS ix_products_restaurant_id ON products (restaurant_id);
                CREATE INDEX IF NOT EXISTS ix_restaurants_owner_id ON restaurants (owner_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "order_items");
            migrationBuilder.DropTable(name: "orders");
            migrationBuilder.DropTable(name: "products");
            migrationBuilder.DropTable(name: "product_categories");
            migrationBuilder.DropTable(name: "restaurants");
        }
    }
}
