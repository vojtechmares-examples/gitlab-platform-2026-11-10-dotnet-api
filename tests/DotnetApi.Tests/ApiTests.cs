using System.Net;
using System.Net.Http.Json;
using DotnetApi.Data;
using DotnetApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetApi.Tests;

/// <summary>The whole HTTP pipeline (routing, validation, JSON, Swagger) in memory, with an in-memory database.</summary>
public class ApiTests : IClassFixture<ApiTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace the PostgreSQL registration of AppDbContext (EF Core 9+ keeps two descriptors per context)
                foreach (var descriptor in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                             d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList())
                {
                    services.Remove(descriptor);
                }
                var name = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(name));
            });
        }
    }

    private readonly HttpClient _client;

    public ApiTests(Factory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Info_reports_the_environment()
    {
        var info = await _client.GetFromJsonAsync<Dictionary<string, object>>("/api/info");

        Assert.NotNull(info);
        Assert.Equal("dotnet-api", info["application"].ToString());
        Assert.Equal("Testing", info["environment"].ToString());
    }

    [Fact]
    public async Task A_task_can_be_created_read_and_deleted()
    {
        var created = await _client.PostAsJsonAsync("/api/todos", new { title = "ship it" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoItem>();
        Assert.NotNull(todo);

        var read = await _client.GetFromJsonAsync<TodoItem>($"/api/todos/{todo.Id}");
        Assert.Equal("ship it", read!.Title);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/todos/{todo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/todos/{todo.Id}")).StatusCode);
    }

    [Fact]
    public async Task An_empty_title_is_rejected_with_400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_describes_the_api()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/Todos", json);
        Assert.Contains("TodoItem", json);
    }
}
