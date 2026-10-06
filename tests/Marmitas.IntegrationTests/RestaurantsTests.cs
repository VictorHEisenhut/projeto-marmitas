using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marmitas.Domain.Entities;
using Marmitas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Marmitas.IntegrationTests;

public class RestaurantsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RestaurantsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Restaurants");
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetAll_DeveRetornarRestaurantesAprovados()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User("Dono", "dono@test.com", null, "hash");
        context.Users.Add(user);
        var restaurant = new Restaurant("Restaurante Teste", "Descricao", "12345678901", user.Id);
        restaurant.Approve();
        var pendingRestaurant = new Restaurant("Pendente", "Desc", "09876543210", user.Id);
        context.Restaurants.Add(restaurant);
        context.Restaurants.Add(pendingRestaurant);
        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/restaurantes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.EnumerateArray().ToList();
        Assert.Contains(data, r => r.GetProperty("name").GetString() == "Restaurante Teste");
        Assert.DoesNotContain(data, r => r.GetProperty("name").GetString() == "Pendente");
    }

    [Fact]
    public async Task GetProducts_DeveRetornarProdutosAtivosDoRestaurante()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User("Dono", "dono2@test.com", null, "hash");
        context.Users.Add(user);

        var restaurant = new Restaurant("Restaurante Produtos", "Descricao", "12345678902", user.Id);
        restaurant.Approve();
        context.Restaurants.Add(restaurant);

        var category = new ProductCategory(restaurant.Id, "Bebidas", 1);
        context.ProductCategories.Add(category);

        var product1 = new Product(restaurant.Id, category.Id, "Coca-Cola", "Refrigerante lata", 5.0m, null);
        var product2 = new Product(restaurant.Id, category.Id, "Fanta", "Refrigerante lata", 5.0m, null);
        product2.Deactivate();

        context.Products.Add(product1);
        context.Products.Add(product2);

        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/restaurantes/{restaurant.Id}/cardapio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var data = json.EnumerateArray().ToList();
        Assert.Single(data); // Só 1 produto ativo
        Assert.Equal("Coca-Cola", data[0].GetProperty("name").GetString());
        Assert.Equal("Bebidas", data[0].GetProperty("categoryName").GetString());
    }
}

