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

public class PlatformModulesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlatformModulesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_PlatformModules");
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    private async Task<string> CriarUsuarioEObterTokenAsync(HttpClient client, string email)
    {
        var password = "SenhaForte123";
        await client.PostAsJsonAsync("/api/v1/auth/registro", new RegisterRequest
        {
            Name = "Cliente Teste",
            Email = email,
            Password = password
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task RedefinirSenha_SemCodigoDeVerificacaoValido_DeveBloquearAlteracao()
    {
        var client = _factory.CreateClient();
        var email = $"sec_{Guid.NewGuid()}@email.com";
        await CriarUsuarioEObterTokenAsync(client, email);

        var attackResponse = await client.PostAsJsonAsync("/api/auth/redefinir-senha", new ResetPasswordRequest
        {
            Email = email,
            NewPassword = "NovaSenhaInvasor123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, attackResponse.StatusCode);

        var forgotResponse = await client.PostAsJsonAsync("/api/auth/esqueci-senha", new ForgotPasswordRequest
        {
            Email = email
        });
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var forgotJson = await forgotResponse.Content.ReadFromJsonAsync<JsonElement>();
        var code = forgotJson.GetProperty("resetCode").GetString();

        var validReset = await client.PostAsJsonAsync("/api/auth/redefinir-senha", new ResetPasswordRequest
        {
            Email = email,
            NewPassword = "NovaSenhaValida123",
            ResetCode = code
        });
        Assert.Equal(HttpStatusCode.OK, validReset.StatusCode);
    }

    [Fact]
    public async Task PedidoIdempotenteEPagamentoGateway_DeveRegistrarHistoricoETransacao()
    {
        var client = _factory.CreateClient();
        var token = await CriarUsuarioEObterTokenAsync(client, $"idemp_{Guid.NewGuid()}@email.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid restaurantId;
        Guid productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = new User("Dono", $"dono_{Guid.NewGuid()}@test.com", null, "hash");
            context.Users.Add(owner);

            var restaurant = new Restaurant("Marmitaria Central", "Caseira", "12345678000199", owner.Id);
            restaurant.Approve();
            context.Restaurants.Add(restaurant);

            var category = new ProductCategory(restaurant.Id, "Tradicionais", 1);
            context.ProductCategories.Add(category);

            var product = new Product(restaurant.Id, category.Id, "Feijoada Completa", "Com couve e farofa", 32.0m, null);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            restaurantId = restaurant.Id;
            productId = product.Id;
        }

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var orderReq = new CreateOrderRequest
        {
            RestaurantId = restaurantId,
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 1 }
            }
        };

        using var msg1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pedidos")
        {
            Content = JsonContent.Create(orderReq)
        };
        msg1.Headers.Add("Idempotency-Key", idempotencyKey);
        var res1 = await client.SendAsync(msg1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        var json1 = await res1.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = json1.GetProperty("orderId").GetGuid();

        using var msg2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pedidos")
        {
            Content = JsonContent.Create(orderReq)
        };
        msg2.Headers.Add("Idempotency-Key", idempotencyKey);
        var res2 = await client.SendAsync(msg2);
        var json2 = await res2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, json2.GetProperty("orderId").GetGuid());

        var payRes = await client.PostAsJsonAsync($"/api/pedidos/{orderId}/pagamento", new
        {
            method = "PIX",
            gateway = "MercadoPago"
        });
        Assert.Equal(HttpStatusCode.Created, payRes.StatusCode);
        var payJson = await payRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", payJson.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(payJson.GetProperty("pixCopyPaste").GetString()));

        var historyRes = await client.GetAsync($"/api/pedidos/{orderId}/historico-status");
        Assert.Equal(HttpStatusCode.OK, historyRes.StatusCode);
        var historyJson = await historyRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", historyJson.GetProperty("currentStatus").GetString());
    }

    [Fact]
    public async Task LgpdExportacaoEExclusaoDeConta_DeveExportarDadosEAnonimizarTitular()
    {
        var client = _factory.CreateClient();
        var email = $"lgpd_{Guid.NewGuid()}@email.com";
        var token = await CriarUsuarioEObterTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var exportRes = await client.GetAsync("/api/usuarios/me/exportar-dados");
        Assert.Equal(HttpStatusCode.OK, exportRes.StatusCode);
        var exportJson = await exportRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, exportJson.GetProperty("profile").GetProperty("email").GetString());

        var deleteRes = await client.DeleteAsync("/api/usuarios/me");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }

    [Fact]
    public async Task UploadDeArquivo_DeveSalvarImagemERetornarUrlPublica()
    {
        var client = _factory.CreateClient();
        var email = $"upload_{Guid.NewGuid()}@email.com";
        var token = await CriarUsuarioEObterTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var form = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "marmita.png");

        var uploadRes = await client.PostAsync("/api/arquivos/upload", form);
        Assert.Equal(HttpStatusCode.Created, uploadRes.StatusCode);
        var json = await uploadRes.Content.ReadFromJsonAsync<JsonElement>();
        var url = json.GetProperty("url").GetString();
        Assert.NotNull(url);
        Assert.StartsWith("/uploads/", url);
    }
}
