using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TemenosAlertManager.Api;
using TemenosAlertManager.Tests.Playwright.Infrastructure;

namespace TemenosAlertManager.Tests.Playwright.Tests.Api;

[TestFixture]
public class HealthControllerTests
{
    private TestWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetDashboard_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
        
        // Verify it's valid JSON
        Assert.DoesNotThrow(() => JsonSerializer.Deserialize<object>(content));
    }

    [Test]
    public async Task GetDashboard_ReturnsValidContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/health/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task GetSummary_WithValidDomain_ReturnsOk()
    {
        // Arrange
        var domain = "Test";

        // Act
        var response = await _client.GetAsync($"/api/health/summary/{domain}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetSummary_WithQueryParameters_ReturnsOk()
    {
        // Arrange
        var domain = "Test";
        var limit = 50;

        // Act
        var response = await _client.GetAsync($"/api/health/summary/{domain}?limit={limit}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task RunCheck_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Domain = "Test",
            Target = "localhost",
            Parameters = new Dictionary<string, object>
            {
                { "timeout", 30 }
            }
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/health/checks/run", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.That(responseContent, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task RunCheck_WithEmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/health/checks/run", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task RunCheck_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var content = new StringContent("invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/health/checks/run", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task HealthController_AllEndpoints_RespondWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        _client.Timeout = timeout;

        var endpoints = new[]
        {
            "/api/health/dashboard",
            "/api/health/summary/Test",
            "/api/health/summary/Test?limit=10"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                $"Endpoint {endpoint} failed with status {response.StatusCode}");
        }
    }

    [Test]
    public async Task HealthController_ConcurrentRequests_HandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 10;

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/api/health/dashboard"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            response.Dispose();
        }
    }
}