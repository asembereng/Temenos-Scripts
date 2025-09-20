using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TemenosAlertManager.Api;
using TemenosAlertManager.Tests.Playwright.Infrastructure;

namespace TemenosAlertManager.Tests.Playwright.Tests.Api;

[TestFixture]
public class AlertsControllerTests
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
    public async Task GetActiveAlerts_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts/active");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
        
        // Verify it's valid JSON array
        Assert.DoesNotThrow(() => JsonSerializer.Deserialize<object[]>(content));
    }

    [Test]
    public async Task GetAlerts_WithDefaultParameters_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
        
        // Check pagination headers
        Assert.That(response.Headers.Contains("X-Total-Count"), Is.True);
        Assert.That(response.Headers.Contains("X-Page"), Is.True);
        Assert.That(response.Headers.Contains("X-Page-Size"), Is.True);
    }

    [Test]
    public async Task GetAlerts_WithPaginationParameters_ReturnsOk()
    {
        // Arrange
        var page = 2;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"/api/alerts?page={page}&pageSize={pageSize}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Check pagination headers
        Assert.That(response.Headers.GetValues("X-Page").First(), Is.EqualTo(page.ToString()));
        Assert.That(response.Headers.GetValues("X-Page-Size").First(), Is.EqualTo(pageSize.ToString()));
    }

    [Test]
    public async Task GetAlerts_WithStateFilter_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts?state=Active");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetAlerts_WithSeverityFilter_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts?severity=Critical");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetAlerts_WithDomainFilter_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts?domain=TPH");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetAlerts_WithSinceFilter_ReturnsOk()
    {
        // Arrange
        var since = DateTime.UtcNow.AddDays(-7);
        var sinceString = since.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Act
        var response = await _client.GetAsync($"/api/alerts?since={sinceString}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetAlerts_WithMultipleFilters_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/alerts?state=Active&severity=Critical&page=1&pageSize=5");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetAlert_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/alerts/{nonExistentId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetAlert_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var alertId = 1;

        // Act
        var response = await _client.GetAsync($"/api/alerts/{alertId}");

        // Assert
        // Since we don't have seeded data, we expect either OK (if alert exists) or NotFound
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AcknowledgeAlert_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var alertId = 1;
        var request = new
        {
            Notes = "Test acknowledgment"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/alerts/{alertId}/acknowledge", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AlertsController_AllGetEndpoints_RespondWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        _client.Timeout = timeout;

        var endpoints = new[]
        {
            "/api/alerts",
            "/api/alerts/active",
            "/api/alerts?page=1&pageSize=10",
            "/api/alerts?state=Active",
            "/api/alerts?severity=Critical"
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
    public async Task AlertsController_ConcurrentRequests_HandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 10;

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/api/alerts/active"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            response.Dispose();
        }
    }

    [Test]
    public async Task AlertsController_ValidContentTypes_ReturnsJson()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/alerts",
            "/api/alerts/active"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        }
    }
}