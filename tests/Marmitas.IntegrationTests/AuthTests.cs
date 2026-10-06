using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marmitas.Application.Users.Dtos;
using Marmitas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Marmitas.IntegrationTests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {

                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Register_ComDadosValidos_DeveRetornar201()
    {
        var client = _factory.CreateClient();
        var email = $"teste_{Guid.NewGuid()}@email.com";
        var request = new RegisterRequest
        {
            Name = "Teste",
            Email = email,
            Phone = "11999999999",
            Password = "SenhaForte123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/registro", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json;
        Assert.Equal(email, data.GetProperty("email").GetString());
        Assert.Contains("customer", data.GetProperty("roles").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Login_ComCredenciaisCorretas_DeveRetornarToken()
    {
        var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid()}@email.com";
        var password = "SenhaForte123";

        await client.PostAsJsonAsync("/api/auth/registro", new RegisterRequest
        {
            Name = "Teste Login",
            Email = email,
            Password = password
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json;
        Assert.False(string.IsNullOrEmpty(data.GetProperty("accessToken").GetString()));
        Assert.Equal("Bearer", data.GetProperty("tokenType").GetString());
        Assert.Equal(email, data.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_ComEmailJaExistente_DeveRetornar409Conflict()
    {
        var client = _factory.CreateClient();
        var email = $"duplicate_{Guid.NewGuid()}@email.com";
        var request = new RegisterRequest
        {
            Name = "Duplicate",
            Email = email,
            Password = "SenhaForte123"
        };

        await client.PostAsJsonAsync("/api/auth/registro", request);

        var response = await client.PostAsJsonAsync("/api/auth/registro", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ERROR", json.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_DeveRetornar401Unauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"wrong_{Guid.NewGuid()}@email.com";

        await client.PostAsJsonAsync("/api/auth/registro", new RegisterRequest
        {
            Name = "Wrong Pass",
            Email = email,
            Password = "SenhaForte123"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "SenhaErrada456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ERROR", json.GetProperty("errors")[0].GetProperty("code").GetString());
    }
}

