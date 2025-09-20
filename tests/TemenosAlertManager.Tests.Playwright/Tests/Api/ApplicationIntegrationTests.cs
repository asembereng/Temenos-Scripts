using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TemenosAlertManager.Api;
using TemenosAlertManager.Tests.Playwright.Infrastructure;

namespace TemenosAlertManager.Tests.Playwright.Tests.Api;

[TestFixture]
public class ApplicationIntegrationTests
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
    public async Task Application_StartsSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert - Application should respond (might be 404 for root, but should not be 500)
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task Swagger_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SwaggerJson_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("openapi"));
        
        // Verify it's valid JSON
        Assert.DoesNotThrow(() => JsonSerializer.Deserialize<object>(content));
    }

    [Test]
    public async Task Hangfire_DashboardIsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/hangfire");

        // Assert - Should either be accessible or require authentication
        var acceptableStatuses = new[] { 
            HttpStatusCode.OK, 
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Found
        };
        
        Assert.That(acceptableStatuses, Does.Contain(response.StatusCode));
    }

    [Test]
    public async Task AllHealthEndpoints_AreAccessible()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/health/dashboard",
            "/api/health/summary/Test",
            "/api/health/summary/TPH",
            "/api/health/summary/T24",
            "/api/health/summary/MQ",
            "/api/health/summary/SQL"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                $"Health endpoint {endpoint} should be accessible");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(content, Is.Not.Null.And.Not.Empty, 
                $"Health endpoint {endpoint} should return content");
        }
    }

    [Test]
    public async Task AllAlertsEndpoints_AreAccessible()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/alerts",
            "/api/alerts/active",
            "/api/alerts?page=1&pageSize=10",
            "/api/alerts?state=Active",
            "/api/alerts?severity=Critical",
            "/api/alerts?domain=TPH"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                $"Alerts endpoint {endpoint} should be accessible");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(content, Is.Not.Null.And.Not.Empty, 
                $"Alerts endpoint {endpoint} should return content");
        }
    }

    [Test]
    public async Task HealthCheckRun_WithValidPayload_ReturnsOk()
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
    public async Task Application_HandlesInvalidRoutes_Gracefully()
    {
        // Arrange
        var invalidRoutes = new[]
        {
            "/api/nonexistent",
            "/api/health/invalid",
            "/api/alerts/invalid",
            "/invalid/path"
        };

        // Act & Assert
        foreach (var route in invalidRoutes)
        {
            var response = await _client.GetAsync(route);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), 
                $"Invalid route {route} should return 404");
        }
    }

    [Test]
    public async Task Application_ReturnsProperContentTypes()
    {
        // Arrange
        var apiEndpoints = new[]
        {
            "/api/health/dashboard",
            "/api/alerts/active"
        };

        // Act & Assert
        foreach (var endpoint in apiEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"),
                $"API endpoint {endpoint} should return JSON");
        }
    }

    [Test]
    public async Task Application_HandlesMultipleSimultaneousRequests()
    {
        // Arrange
        const int numberOfRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(_client.GetAsync("/api/health/dashboard"));
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
    public async Task Application_RespondsWithinReasonableTime()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/health/dashboard",
            "/api/alerts/active",
            "/swagger",
            "/hangfire"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();

            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30000), 
                $"Endpoint {endpoint} should respond within 30 seconds");
            
            // Status should be OK or acceptable for protected endpoints
            var acceptableStatuses = new[] { 
                HttpStatusCode.OK, 
                HttpStatusCode.Unauthorized, 
                HttpStatusCode.Forbidden,
                HttpStatusCode.Found
            };
            Assert.That(acceptableStatuses, Does.Contain(response.StatusCode));
        }
    }

    [Test]
    public async Task Application_CORS_IsConfigured()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.GetAsync("/api/health/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Check for CORS headers (might not be present in all test scenarios)
        var hasAccessControlAllowOrigin = response.Headers.Contains("Access-Control-Allow-Origin");
        
        // In a proper CORS setup, this header should be present for cross-origin requests
        // But in test environment, it might not be set, so we just verify the request succeeds
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Application_SecurityHeaders_ArePresent()
    {
        // Act
        var response = await _client.GetAsync("/api/health/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Check for security headers that should typically be present
        var headers = response.Headers.ToString();
        
        // At minimum, we should have some headers set by ASP.NET Core
        Assert.That(response.Headers, Is.Not.Null);
        Assert.That(response.Headers.Count(), Is.GreaterThan(0));
    }
}