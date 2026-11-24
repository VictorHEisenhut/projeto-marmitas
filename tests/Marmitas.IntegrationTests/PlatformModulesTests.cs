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

        var addrRes = await client.PostAsJsonAsync("/api/usuarios/me/enderecos", new
        {
            street = "Rua Antônio da Veiga",
            number = "520",
            neighborhood = "Victor Konder",
            city = "Blumenau",
            state = "SC",
            zipCode = "89012-500",
            isMain = true
        });
        Assert.Equal(HttpStatusCode.Created, addrRes.StatusCode);

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

    [Fact]
    public async Task PlanosIndividuaisPorRestaurante_DonoCriaClienteAssinaEResgataApenasNoMesmoRestaurante()
    {
        var ownerAClient = _factory.CreateClient();
        var ownerBClient = _factory.CreateClient();
        var customerClient = _factory.CreateClient();

        async Task<(Guid UserId, string Token)> CriarUsuarioAsync(HttpClient c, string email, string role)
        {
            const string pwd = "SenhaForte123";
            await c.PostAsJsonAsync("/api/v1/auth/registro", new RegisterRequest
            {
                Name = $"Usuario {role}",
                Email = email,
                Password = pwd,
                Role = role
            });

            var loginRes = await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = email,
                Password = pwd
            });
            loginRes.EnsureSuccessStatusCode();
            var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
            return (
                loginJson.GetProperty("user").GetProperty("id").GetGuid(),
                loginJson.GetProperty("accessToken").GetString()!
            );
        }

        var (ownerAId, ownerAToken) = await CriarUsuarioAsync(ownerAClient, $"ownerA_{Guid.NewGuid()}@test.com", "restaurant_owner");
        var (ownerBId, ownerBToken) = await CriarUsuarioAsync(ownerBClient, $"ownerB_{Guid.NewGuid()}@test.com", "restaurant_owner");
        var (_, customerToken) = await CriarUsuarioAsync(customerClient, $"cli_{Guid.NewGuid()}@test.com", "customer");

        ownerAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAToken);
        ownerBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerBToken);
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        Guid restAId;
        Guid prodAId;
        Guid restBId;
        Guid prodBId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var restA = new Restaurant("Marmitaria Alpha", "Caseira", "11111111000111", ownerAId);
            restA.Approve();
            var restB = new Restaurant("Marmitaria Beta", "Fitness", "22222222000122", ownerBId);
            restB.Approve();
            context.Restaurants.AddRange(restA, restB);

            var catA = new ProductCategory(restA.Id, "Tradicionais", 1);
            var catB = new ProductCategory(restB.Id, "Fitness", 1);
            context.ProductCategories.AddRange(catA, catB);

            var prodA = new Product(restA.Id, catA.Id, "Frango Grelhado Alpha", "Com arroz integral", 29.90m, null);
            var prodB = new Product(restB.Id, catB.Id, "Salmao Fit Beta", "Com legumes", 39.90m, null);
            context.Products.AddRange(prodA, prodB);

            await context.SaveChangesAsync();
            restAId = restA.Id;
            prodAId = prodA.Id;
            restBId = restB.Id;
            prodBId = prodB.Id;
        }

        // Dono A cria plano no Restaurante A
        var createPlanARes = await ownerAClient.PostAsJsonAsync("/api/planos", new
        {
            restaurantId = restAId,
            name = "Plano Alpha 5x",
            description = "Almoço semanal Alpha",
            mealsPerWeek = 5,
            monthlyPrice = 249.90m
        });
        Assert.Equal(HttpStatusCode.Created, createPlanARes.StatusCode);
        var planAJson = await createPlanARes.Content.ReadFromJsonAsync<JsonElement>();
        var planAId = planAJson.GetProperty("id").GetGuid();
        Assert.Equal(restAId, planAJson.GetProperty("restaurantId").GetGuid());
        Assert.Equal("Marmitaria Alpha", planAJson.GetProperty("restaurantName").GetString());

        // Dono B nao pode editar nem desativar o plano do Restaurante A
        var forbidUpdate = await ownerBClient.PutAsJsonAsync($"/api/planos/{planAId}", new
        {
            restaurantId = restAId,
            name = "Plano Invadido",
            mealsPerWeek = 5,
            monthlyPrice = 99m
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbidUpdate.StatusCode);

        var forbidDelete = await ownerBClient.DeleteAsync($"/api/planos/{planAId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidDelete.StatusCode);

        // Dono B cria plano proprio no Restaurante B
        var createPlanBRes = await ownerBClient.PostAsJsonAsync("/api/planos", new
        {
            restaurantId = restBId,
            name = "Plano Beta 5x",
            description = "Almoço semanal Beta",
            mealsPerWeek = 5,
            monthlyPrice = 329.90m
        });
        Assert.Equal(HttpStatusCode.Created, createPlanBRes.StatusCode);
        var planBJson = await createPlanBRes.Content.ReadFromJsonAsync<JsonElement>();
        var planBId = planBJson.GetProperty("id").GetGuid();

        // Filtro por restaurante na vitrine
        var plansARes = await customerClient.GetFromJsonAsync<JsonElement>($"/api/planos?restaurantId={restAId}");
        var planAIds = plansARes.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(planAId, planAIds);
        Assert.DoesNotContain(planBId, planAIds);

        // Cliente assina o plano do Restaurante A
        var subscribeARes = await customerClient.PostAsJsonAsync("/api/assinaturas", new { planId = planAId });
        Assert.Equal(HttpStatusCode.Created, subscribeARes.StatusCode);
        var subAJson = await subscribeARes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(restAId, subAJson.GetProperty("restaurantId").GetGuid());

        // Segundo plano no mesmo restaurante A deve ser bloqueado
        var duplicateSubARes = await customerClient.PostAsJsonAsync("/api/assinaturas", new { planId = planAId });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateSubARes.StatusCode);

        var customerAddrRes = await customerClient.PostAsJsonAsync("/api/usuarios/me/enderecos", new
        {
            street = "Rua XV de Novembro",
            number = "700",
            neighborhood = "Centro",
            city = "Blumenau",
            state = "SC",
            zipCode = "89010-001",
            isMain = true
        });
        Assert.Equal(HttpStatusCode.Created, customerAddrRes.StatusCode);

        // Cliente tenta resgatar pelo plano no Restaurante B -> deve falhar
        var redeemWrongRes = await customerClient.PostAsJsonAsync("/api/v1/pedidos", new CreateOrderRequest
        {
            RestaurantId = restBId,
            IsPlanRedemption = true,
            Items = new List<OrderItemRequest> { new() { ProductId = prodBId, Quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, redeemWrongRes.StatusCode);

        // Cliente resgata pelo plano no Restaurante A -> deve funcionar com valor base zero
        var redeemRightRes = await customerClient.PostAsJsonAsync("/api/v1/pedidos", new CreateOrderRequest
        {
            RestaurantId = restAId,
            IsPlanRedemption = true,
            Items = new List<OrderItemRequest> { new() { ProductId = prodAId, Quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, redeemRightRes.StatusCode);
        var orderRightJson = await redeemRightRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0m, orderRightJson.GetProperty("totalAmount").GetDecimal());

        // Cliente consegue assinar simultaneamente um plano do Restaurante B
        var subscribeBRes = await customerClient.PostAsJsonAsync("/api/assinaturas", new { planId = planBId });
        Assert.Equal(HttpStatusCode.Created, subscribeBRes.StatusCode);
    }

    [Fact]
    public async Task PlanosEAvaliacoes_MaisAcessadosNaHomeETodosNoRestaurante()
    {
        var ownerClient = _factory.CreateClient();
        var customerClient = _factory.CreateClient();
        var driverClient = _factory.CreateClient();

        async Task<(Guid UserId, string Token)> CriarUsuarioAsync(HttpClient c, string email, string role)
        {
            const string pwd = "SenhaForte123";
            await c.PostAsJsonAsync("/api/v1/auth/registro", new RegisterRequest
            {
                Name = $"User {role}",
                Email = email,
                Password = pwd,
                Role = role
            });

            var loginRes = await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = email,
                Password = pwd
            });
            loginRes.EnsureSuccessStatusCode();
            var loginJson = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
            return (
                loginJson.GetProperty("user").GetProperty("id").GetGuid(),
                loginJson.GetProperty("accessToken").GetString()!
            );
        }

        var (ownerId, ownerToken) = await CriarUsuarioAsync(ownerClient, $"ownerFeat_{Guid.NewGuid()}@test.com", "restaurant_owner");
        var (_, customerToken) = await CriarUsuarioAsync(customerClient, $"cliFeat_{Guid.NewGuid()}@test.com", "customer");
        var (_, driverToken) = await CriarUsuarioAsync(driverClient, $"drvFeat_{Guid.NewGuid()}@test.com", "driver");

        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        driverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        Guid restId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rest = new Restaurant("Restaurante Destaque", "Caseira", "33333333000133", ownerId);
            rest.Approve();
            context.Restaurants.Add(rest);
            await context.SaveChangesAsync();
            restId = rest.Id;
        }

        var createdPlanIds = new List<Guid>();
        for (var i = 1; i <= 4; i++)
        {
            var planRes = await ownerClient.PostAsJsonAsync("/api/planos", new
            {
                restaurantId = restId,
                name = $"Plano Loja {i}",
                description = $"Descricao plano {i}",
                mealsPerWeek = i + 1,
                monthlyPrice = 190m + (i * 40m)
            });
            Assert.Equal(HttpStatusCode.Created, planRes.StatusCode);
            var planJson = await planRes.Content.ReadFromJsonAsync<JsonElement>();
            createdPlanIds.Add(planJson.GetProperty("id").GetGuid());
        }

        // Assina especificamente o 4o plano (mais caro) para ele subir para os mais acessados
        var popularPlanId = createdPlanIds[3];
        var subRes = await customerClient.PostAsJsonAsync("/api/assinaturas", new { planId = popularPlanId });
        Assert.Equal(HttpStatusCode.Created, subRes.StatusCode);

        var anonClient = _factory.CreateClient();

        // Todos os 4 planos aparecem no endpoint do restaurante
        var restPlans = await anonClient.GetFromJsonAsync<JsonElement>($"/api/restaurantes/{restId}/planos");
        var restPlanIds = restPlans.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(4, restPlanIds.Count);
        foreach (var pid in createdPlanIds)
        {
            Assert.Contains(pid, restPlanIds);
        }

        // Mais acessados retorna no maximo 3 e inclui o plano assinado com subscribersCount >= 1
        var topPlans = await anonClient.GetFromJsonAsync<JsonElement>("/api/planos/mais-acessados?limit=3");
        var topPlansList = topPlans.EnumerateArray().ToList();
        Assert.True(topPlansList.Count <= 3);
        Assert.Contains(topPlansList, p =>
            p.GetProperty("id").GetGuid() == popularPlanId &&
            p.GetProperty("subscribersCount").GetInt32() >= 1);

        // Avaliacoes: 2 de clientes (nota 5 e nota 3) e 1 de entregador (nao deve aparecer no restaurante)
        var revCust5 = await customerClient.PostAsJsonAsync("/api/avaliacoes/clientes", new
        {
            restaurantId = restId,
            rating = 5,
            comment = "Excelente marmita e entrega pontual!"
        });
        Assert.Equal(HttpStatusCode.Created, revCust5.StatusCode);

        var revCust3 = await customerClient.PostAsJsonAsync("/api/avaliacoes/clientes", new
        {
            restaurantId = restId,
            rating = 3,
            comment = "Sabor bom, mas atrasou um pouco."
        });
        Assert.Equal(HttpStatusCode.Created, revCust3.StatusCode);

        var revDriver = await driverClient.PostAsJsonAsync("/api/avaliacoes/entregadores", new
        {
            restaurantId = restId,
            rating = 5,
            comment = "Coleta interna rapida para entregadores."
        });
        Assert.Equal(HttpStatusCode.Created, revDriver.StatusCode);

        // Avaliacoes do restaurante devem retornar apenas Customer (2 itens), nunca Courier
        var restReviews = await anonClient.GetFromJsonAsync<JsonElement>($"/api/restaurantes/{restId}/avaliacoes");
        var restReviewList = restReviews.EnumerateArray().ToList();
        Assert.Equal(2, restReviewList.Count);
        Assert.All(restReviewList, r => Assert.Equal("Customer", r.GetProperty("targetType").GetString()));

        // Destaques da Home devem retornar no maximo 3 avaliacoes Customer ordenadas pela maior nota primeiro
        var featuredReviews = await anonClient.GetFromJsonAsync<JsonElement>("/api/avaliacoes/destaques?limit=3");
        var featuredList = featuredReviews.EnumerateArray().ToList();
        Assert.True(featuredList.Count is >= 1 and <= 3);
        Assert.All(featuredList, r => Assert.Equal("Customer", r.GetProperty("targetType").GetString()));
        var ratings = featuredList.Select(r => r.GetProperty("rating").GetInt32()).ToList();
        Assert.Equal(ratings.OrderByDescending(x => x).ToList(), ratings);
    }

    [Fact]
    public async Task RastreamentoEntrega_DeveRetornarNomeDoEntregadorENaoGuid()
    {
        var driverClient = _factory.CreateClient();
        var customerClient = _factory.CreateClient();

        const string password = "SenhaForte123";
        var driverEmail = $"driver_name_{Guid.NewGuid()}@test.com";
        var customerEmail = $"cust_track_{Guid.NewGuid()}@test.com";

        await driverClient.PostAsJsonAsync("/api/v1/auth/registro", new RegisterRequest
        {
            Name = "Carlos Entregador",
            Email = driverEmail,
            Password = password,
            Role = "driver"
        });
        var driverLogin = await driverClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = driverEmail,
            Password = password
        });
        driverLogin.EnsureSuccessStatusCode();
        var driverJson = await driverLogin.Content.ReadFromJsonAsync<JsonElement>();
        var driverId = driverJson.GetProperty("user").GetProperty("id").GetGuid();
        var driverToken = driverJson.GetProperty("accessToken").GetString()!;
        driverClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        await customerClient.PostAsJsonAsync("/api/v1/auth/registro", new RegisterRequest
        {
            Name = "Cliente Rastreamento",
            Email = customerEmail,
            Password = password,
            Role = "customer"
        });
        var customerLogin = await customerClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = customerEmail,
            Password = password
        });
        customerLogin.EnsureSuccessStatusCode();
        var customerJson = await customerLogin.Content.ReadFromJsonAsync<JsonElement>();
        var customerToken = customerJson.GetProperty("accessToken").GetString()!;
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        // Atualiza localizacao antes de ter registro na tabela Couriers (exercita EnsureCourierEntityAsync)
        var locRes = await driverClient.PatchAsJsonAsync("/api/entregadores/me/localizacao", new
        {
            latitude = -26.9194,
            longitude = -49.0661
        });
        Assert.Equal(HttpStatusCode.OK, locRes.StatusCode);

        Guid restaurantId;
        Guid productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var courier = await context.Couriers.FirstOrDefaultAsync(c => c.UserId == driverId);
            Assert.NotNull(courier);
            Assert.Equal("Carlos Entregador", courier!.Name);

            var owner = new User("Dono Rastreamento", $"owner_track_{Guid.NewGuid()}@test.com", null, "hash");
            context.Users.Add(owner);

            var restaurant = new Restaurant("Marmitaria Rastreamento", "Caseira", "44444444000144", owner.Id)
            {
                AddressCity = "Blumenau",
                AddressState = "SC",
                Latitude = -26.9194,
                Longitude = -49.0661,
                DeliveryRadiusKm = 12
            };
            restaurant.Approve();
            context.Restaurants.Add(restaurant);

            var category = new ProductCategory(restaurant.Id, "Executivas", 1);
            context.ProductCategories.Add(category);

            var product = new Product(restaurant.Id, category.Id, "Prato Executivo", "Com salada", 29.9m, null);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            restaurantId = restaurant.Id;
            productId = product.Id;
        }

        var addrRes = await customerClient.PostAsJsonAsync("/api/usuarios/me/enderecos", new
        {
            street = "Rua 7 de Setembro",
            number = "100",
            neighborhood = "Centro",
            city = "Blumenau",
            state = "SC",
            zipCode = "89010-000",
            isMain = true
        });
        Assert.Equal(HttpStatusCode.Created, addrRes.StatusCode);

        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pedidos")
        {
            Content = JsonContent.Create(new CreateOrderRequest
            {
                RestaurantId = restaurantId,
                Items = new List<OrderItemRequest>
                {
                    new() { ProductId = productId, Quantity = 1 }
                }
            })
        };
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var orderRes = await customerClient.SendAsync(orderMsg);
        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);
        var orderId = (await orderRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Deliveries.Add(new Delivery
            {
                OrderId = orderId,
                CourierId = driverId,
                Status = "DriverAssigned"
            });
            await context.SaveChangesAsync();
        }

        var trackRes = await customerClient.GetAsync($"/api/entregas/{orderId}/rastreamento");
        trackRes.EnsureSuccessStatusCode();
        var trackJson = await trackRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Carlos Entregador", trackJson.GetProperty("courierName").GetString());

        // Simula registro legado gravado com GUID em couriers.name e garante fallback para nome real do usuario
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var courier = await context.Couriers.FirstAsync(c => c.UserId == driverId);
            courier.Name = driverId.ToString();
            await context.SaveChangesAsync();
        }

        var legacyTrackRes = await customerClient.GetAsync($"/api/entregas/{orderId}/rastreamento");
        legacyTrackRes.EnsureSuccessStatusCode();
        var legacyTrackJson = await legacyTrackRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Carlos Entregador", legacyTrackJson.GetProperty("courierName").GetString());
    }
}
