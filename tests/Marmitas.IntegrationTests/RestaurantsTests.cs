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

    [Fact]
    public async Task GetAll_ComLocalizacao_DeveFiltrarPorRaioECidadeEBloquearPedidoForaDoRaio()
    {
        Guid restBlumenauId;
        Guid restCaieirasId;
        Guid prodCaieirasId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = new User("Dono Geo", $"dono_geo_{Guid.NewGuid()}@test.com", null, "hash");
            context.Users.Add(owner);

            var restBlumenau = new Restaurant("Marmitaria Blumenau", "Caseira", "12345678000101", owner.Id)
            {
                AddressStreet = "Rua XV de Novembro",
                AddressNumber = "450",
                AddressNeighborhood = "Centro",
                AddressCity = "Blumenau",
                AddressState = "SC",
                AddressZipCode = "89010-001",
                Latitude = -26.9194,
                Longitude = -49.0661,
                DeliveryRadiusKm = 8.0
            };
            restBlumenau.Approve();

            var restCaieiras = new Restaurant("Rest Furb SP", "Caseira SP", "12345678000102", owner.Id)
            {
                AddressStreet = "Estrada Santa Inês",
                AddressNumber = "100",
                AddressNeighborhood = "Alpes de Caieiras",
                AddressCity = "Caieiras",
                AddressState = "SP",
                AddressZipCode = "07700-000",
                Latitude = null,
                Longitude = null,
                DeliveryRadiusKm = 12.0
            };
            restCaieiras.Approve();

            context.Restaurants.AddRange(restBlumenau, restCaieiras);

            var cat = new ProductCategory(restCaieiras.Id, "Tradicionais", 1);
            context.ProductCategories.Add(cat);

            var prodCaieiras = new Product(restCaieiras.Id, cat.Id, "Prato Caieiras", "Arroz e feijão", 25.0m, null);
            context.Products.Add(prodCaieiras);

            await context.SaveChangesAsync();
            restBlumenauId = restBlumenau.Id;
            restCaieirasId = restCaieiras.Id;
            prodCaieirasId = prodCaieiras.Id;
        }

        var client = _factory.CreateClient();

        // Cliente em Blumenau/SC só deve ver restaurante de Blumenau/SC
        var blumenauRes = await client.GetFromJsonAsync<JsonElement>("/api/restaurantes?city=Blumenau&state=SC");
        var blumenauList = blumenauRes.EnumerateArray().ToList();
        Assert.Contains(blumenauList, r =>
            r.GetProperty("id").GetGuid() == restBlumenauId &&
            r.GetProperty("distanceKm").GetDouble() <= 8.0);
        Assert.DoesNotContain(blumenauList, r => r.GetProperty("id").GetGuid() == restCaieirasId);

        // Cliente em Caieiras/SP só deve ver restaurante de Caieiras/SP
        var caieirasRes = await client.GetFromJsonAsync<JsonElement>("/api/restaurantes?city=Caieiras&state=SP");
        var caieirasList = caieirasRes.EnumerateArray().ToList();
        Assert.Contains(caieirasList, r => r.GetProperty("id").GetGuid() == restCaieirasId);
        Assert.DoesNotContain(caieirasList, r => r.GetProperty("id").GetGuid() == restBlumenauId);

        // Cliente com endereço em Blumenau tentando pedir no restaurante de Caieiras/SP deve ser bloqueado
        var customerEmail = $"geo_cli_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/registro", new RegisterRequest
        {
            Name = "Cliente Blumenau",
            Email = customerEmail,
            Password = "SenhaForte123"
        });
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = customerEmail,
            Password = "SenhaForte123"
        });
        var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var addrRes = await client.PostAsJsonAsync("/api/usuarios/me/enderecos", new
        {
            street = "Rua XV de Novembro",
            number = "450",
            neighborhood = "Centro",
            city = "Blumenau",
            state = "SC",
            zipCode = "89010-001",
            isMain = true
        });
        Assert.Equal(HttpStatusCode.Created, addrRes.StatusCode);

        var outOfRadiusOrderRes = await client.PostAsJsonAsync("/api/pedidos", new CreateOrderRequest
        {
            RestaurantId = restCaieirasId,
            Items = new List<OrderItemRequest> { new() { ProductId = prodCaieirasId, Quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, outOfRadiusOrderRes.StatusCode);
        var body = System.Text.RegularExpressions.Regex.Unescape(await outOfRadiusOrderRes.Content.ReadAsStringAsync());
        Assert.Contains("fora do raio", body, StringComparison.OrdinalIgnoreCase);
    }
}

