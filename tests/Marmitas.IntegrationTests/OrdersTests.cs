using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Marmitas.Application.Orders.Dtos;
using Marmitas.Application.Users.Dtos;
using Marmitas.Domain.Entities;
using Marmitas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Marmitas.IntegrationTests;

public class OrdersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Orders");
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    private async Task<string> ObterTokenAsync(HttpClient client, string email)
    {
        var password = "SenhaForte123";

        await client.PostAsJsonAsync("/api/auth/registro", new RegisterRequest
        {
            Name = "Teste Orders",
            Email = email,
            Password = password
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task PostOrders_ComDadosValidos_DeveRetornar201ECriarPedido()
    {
        var client = _factory.CreateClient();
        var token = await ObterTokenAsync(client, $"customer_{Guid.NewGuid()}@email.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid restaurantId;
        Guid productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = new User("Dono", $"dono_{Guid.NewGuid()}@test.com", null, "hash");
            context.Users.Add(owner);

            var restaurant = new Restaurant("Restaurante Orders", "Descricao", "12345678901", owner.Id);
            restaurant.Approve();
            context.Restaurants.Add(restaurant);

            var category = new ProductCategory(restaurant.Id, "Pratos", 1);
            context.ProductCategories.Add(category);

            var product = new Product(restaurant.Id, category.Id, "Marmita P", "Marmita pequena", 15.0m, null);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            restaurantId = restaurant.Id;
            productId = product.Id;
        }

        var request = new CreateOrderRequest
        {
            RestaurantId = restaurantId,
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = productId, Quantity = 2 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json;
        Assert.Equal(30.0m, data.GetProperty("totalAmount").GetDecimal());
        Assert.Equal("Created", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostOrders_SemEstarAutenticado_DeveRetornar401()
    {
        var client = _factory.CreateClient();
        var request = new CreateOrderRequest
        {
            RestaurantId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

