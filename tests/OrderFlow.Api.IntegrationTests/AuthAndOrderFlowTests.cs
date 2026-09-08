using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.Api;
using OrderFlow.Application.Auth.Dtos;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.IntegrationTests;

public sealed class OrderFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"OrderFlowTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderFlowDbContext>>();
            services.RemoveAll<OrderFlowDbContext>();
            services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public class AuthAndOrderFlowTests : IClassFixture<OrderFlowApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public AuthAndOrderFlowTests(OrderFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithSeedAdmin_ReturnsTokens()
    {
        var auth = await LoginAsync();
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.Email.Should().Be("admin@orderflow.dev");
    }

    [Fact]
    public async Task Orders_RequireAuthentication()
    {
        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAndConfirmOrder_EndToEnd()
    {
        var auth = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var customers = await _client.GetFromJsonAsync<PagedEnvelope<CustomerEnvelope>>("/api/customers?pageSize=5", JsonOptions);
        var products = await _client.GetFromJsonAsync<PagedEnvelope<ProductEnvelope>>("/api/products?pageSize=5", JsonOptions);
        customers!.Items.Should().NotBeEmpty();
        products!.Items.Should().NotBeEmpty();

        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerId = customers.Items[0].Id,
            notes = "Integration test",
            items = new[] { new { productId = products.Items[0].Id, quantity = 1 } }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        order.Should().NotBeNull();
        order!.Status.ToString().Should().Be("Draft");

        var confirm = await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        confirm.EnsureSuccessStatusCode();
        var confirmed = await confirm.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        confirmed!.Status.ToString().Should().Be("Confirmed");
    }

    private async Task<AuthResponse> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@orderflow.dev",
            password = "Admin@123"
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth.Should().NotBeNull();
        return auth!;
    }

    private sealed record PagedEnvelope<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
    private sealed record CustomerEnvelope(Guid Id, string Name);
    private sealed record ProductEnvelope(Guid Id, string Sku);
}
