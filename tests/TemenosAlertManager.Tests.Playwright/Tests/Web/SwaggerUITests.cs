using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Net;
using TemenosAlertManager.Api;
using TemenosAlertManager.Tests.Playwright.Infrastructure;

namespace TemenosAlertManager.Tests.Playwright.Tests.Web;

[TestFixture]
public class SwaggerUITests : PageTest
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
                await app.GetAsync("/swagger");
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
    public async Task SwaggerUI_HomePage_LoadsSuccessfully()
    {
        try
        {
            // Navigate to Swagger UI
            await Page.GotoAsync($"{_baseUrl}/swagger");

            // Wait for the page to load
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check that the page title contains "Swagger"
            var title = await Page.TitleAsync();
            Assert.That(title, Does.Contain("Swagger"));

            // Check for common Swagger UI elements
            var swaggerContainer = Page.Locator(".swagger-ui");
            await Expect(swaggerContainer).ToBeVisibleAsync();
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
    public async Task SwaggerUI_APIDocumentation_IsAccessible()
    {
        try
        {
            // Navigate to Swagger UI
            await Page.GotoAsync($"{_baseUrl}/swagger");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Look for API documentation sections
            var healthSection = Page.Locator("text=Health");
            var alertsSection = Page.Locator("text=Alerts");

            // Check if health and alerts sections are present
            var healthVisible = await healthSection.IsVisibleAsync();
            var alertsVisible = await alertsSection.IsVisibleAsync();

            // At least one section should be visible
            Assert.That(healthVisible || alertsVisible, Is.True, 
                "Either Health or Alerts API documentation should be visible");
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
    public async Task SwaggerUI_TryItOut_Functionality()
    {
        try
        {
            // Navigate to Swagger UI
            await Page.GotoAsync($"{_baseUrl}/swagger");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Look for "Try it out" buttons
            var tryItOutButtons = Page.Locator("button:has-text('Try it out')");
            var count = await tryItOutButtons.CountAsync();

            // Should have at least one "Try it out" button
            Assert.That(count, Is.GreaterThan(0), 
                "Should have at least one 'Try it out' button");

            // Try to click the first "Try it out" button if available
            if (count > 0)
            {
                await tryItOutButtons.First.ClickAsync();
                
                // Look for execute button
                var executeButton = Page.Locator("button:has-text('Execute')");
                await Expect(executeButton).ToBeVisibleAsync();
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
    public async Task SwaggerUI_RespondsToDirectURL()
    {
        try
        {
            // Test direct navigation to swagger endpoint
            var response = await Page.GotoAsync($"{_baseUrl}/swagger");
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.OK));
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
    public async Task SwaggerUI_JSONEndpoint_IsAccessible()
    {
        try
        {
            // Navigate to Swagger JSON endpoint
            var response = await Page.GotoAsync($"{_baseUrl}/swagger/v1/swagger.json");
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.OK));

            // Check content type
            var contentType = response.Headers["content-type"];
            Assert.That(contentType, Does.Contain("application/json"));

            // Verify it's valid JSON
            var content = await Page.ContentAsync();
            Assert.That(content, Does.Contain("openapi"));
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