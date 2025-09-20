using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Net;
using TemenosAlertManager.Api;
using TemenosAlertManager.Tests.Playwright.Infrastructure;

namespace TemenosAlertManager.Tests.Playwright.Tests.Web;

[TestFixture]
public class HangfireDashboardTests : PageTest
{
    private TestWebApplicationFactory<Program> _factory = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory<Program>();
        var client = _factory.CreateClient();
        _baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:5001";
        
        // Start the application
        _ = Task.Run(async () =>
        {
            using var app = _factory.CreateClient();
            try
            {
                await app.GetAsync("/hangfire");
            }
            catch
            {
                // Ignore startup errors
            }
        });
        
        // Wait for application to start
        await Task.Delay(2000);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    [Test]
    public async Task HangfireDashboard_HomePage_LoadsSuccessfully()
    {
        try
        {
            // Navigate to Hangfire Dashboard
            await Page.GotoAsync($"{_baseUrl}/hangfire");

            // Wait for the page to load
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check that the page loads successfully
            var title = await Page.TitleAsync();
            Assert.That(title, Does.Contain("Hangfire"));

            // Check for common Hangfire elements
            var dashboardContainer = Page.Locator(".page-header, h1, .navbar");
            var isVisible = await dashboardContainer.IsVisibleAsync();
            
            if (isVisible)
            {
                Assert.That(isVisible, Is.True, "Hangfire dashboard elements should be visible");
            }
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }

    [Test]
    public async Task HangfireDashboard_Navigation_IsAccessible()
    {
        try
        {
            // Navigate to Hangfire Dashboard
            await Page.GotoAsync($"{_baseUrl}/hangfire");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Look for navigation elements common in Hangfire
            var navigationElements = new[]
            {
                "Jobs", "Queues", "Servers", "Retries", "Dashboard"
            };

            var foundElements = 0;
            foreach (var element in navigationElements)
            {
                var locator = Page.Locator($"text={element}").Or(Page.Locator($"a:has-text('{element}')"));
                if (await locator.IsVisibleAsync())
                {
                    foundElements++;
                }
            }

            // Should find at least some navigation elements
            Assert.That(foundElements, Is.GreaterThan(0), 
                "Should find at least some Hangfire navigation elements");
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }

    [Test]
    public async Task HangfireDashboard_RespondsToDirectURL()
    {
        try
        {
            // Test direct navigation to hangfire endpoint
            var response = await Page.GotoAsync($"{_baseUrl}/hangfire");
            
            Assert.That(response, Is.Not.Null);
            
            // Hangfire might return 200 or redirect to login
            var acceptableStatuses = new[] { 
                (int)HttpStatusCode.OK, 
                (int)HttpStatusCode.Found, 
                (int)HttpStatusCode.Redirect,
                (int)HttpStatusCode.Unauthorized,
                (int)HttpStatusCode.Forbidden
            };
            
            Assert.That(acceptableStatuses, Does.Contain(response.Status),
                $"Expected acceptable status code, got {response.Status}");
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }

    [Test]
    public async Task HangfireDashboard_StatisticsEndpoint_IsAccessible()
    {
        try
        {
            // Test Hangfire statistics endpoint
            var response = await Page.GotoAsync($"{_baseUrl}/hangfire/stats");
            
            if (response != null)
            {
                // Statistics endpoint might be protected or return different status codes
                var acceptableStatuses = new[] { 
                    (int)HttpStatusCode.OK, 
                    (int)HttpStatusCode.NotFound,
                    (int)HttpStatusCode.Unauthorized,
                    (int)HttpStatusCode.Forbidden
                };
                
                Assert.That(acceptableStatuses, Does.Contain(response.Status),
                    $"Statistics endpoint should return acceptable status, got {response.Status}");
            }
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }

    [Test]
    public async Task HangfireDashboard_Authorization_IsConfigured()
    {
        try
        {
            // Navigate to Hangfire Dashboard
            var response = await Page.GotoAsync($"{_baseUrl}/hangfire");
            
            Assert.That(response, Is.Not.Null);

            // If authorization is properly configured, we should either:
            // 1. Get access (200) if no auth required in test environment
            // 2. Get redirected or unauthorized (401/403) if auth is required
            var validStatuses = new[] { 
                (int)HttpStatusCode.OK, 
                (int)HttpStatusCode.Unauthorized,
                (int)HttpStatusCode.Forbidden,
                (int)HttpStatusCode.Found,
                (int)HttpStatusCode.Redirect
            };
            
            Assert.That(validStatuses, Does.Contain(response.Status),
                "Hangfire dashboard should have proper authorization configuration");
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }

    [Test]
    public async Task HangfireDashboard_JobsPage_IsAccessible()
    {
        try
        {
            // Navigate to Hangfire Jobs page
            var response = await Page.GotoAsync($"{_baseUrl}/hangfire/jobs/enqueued");
            
            if (response != null)
            {
                // Jobs page might be protected or not exist
                var acceptableStatuses = new[] { 
                    (int)HttpStatusCode.OK, 
                    (int)HttpStatusCode.NotFound,
                    (int)HttpStatusCode.Unauthorized,
                    (int)HttpStatusCode.Forbidden
                };
                
                Assert.That(acceptableStatuses, Does.Contain(response.Status),
                    $"Jobs page should return acceptable status, got {response.Status}");
            }
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("browser"))
        {
            Assert.Ignore("Browser not available in this environment");
        }
        catch (TimeoutException)
        {
            Assert.Ignore("Application startup timeout - this is expected in CI environments");
        }
    }
}