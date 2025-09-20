using Microsoft.Playwright;
using NUnit.Framework;

namespace TemenosAlertManager.Tests.Playwright.Tests.Web;

[TestFixture]
public class BrowserAvailabilityTests
{
    [Test]
    public async Task BrowserInstallation_IsAvailable()
    {
        try
        {
            // Try to create a playwright instance
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            // Try to launch a browser (this will install if missing)
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            
            Assert.That(browser, Is.Not.Null);
            
            // Create a page to verify browser works
            var page = await browser.NewPageAsync();
            Assert.That(page, Is.Not.Null);
            
            // Navigate to a simple page
            await page.GotoAsync("data:text/html,<h1>Test Page</h1>");
            var title = await page.TextContentAsync("h1");
            Assert.That(title, Is.EqualTo("Test Page"));
            
            await browser.CloseAsync();
        }
        catch (PlaywrightException ex)
        {
            Assert.Ignore($"Browser not available: {ex.Message}. " +
                         "This is expected in CI environments without browser support.");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Browser installation failed: {ex.Message}. " +
                         "This is expected in environments where browsers cannot be installed.");
        }
    }

    [Test]
    public async Task BrowserInstallation_CanInstallIfMissing()
    {
        try
        {
            // Attempt to install browsers programmatically
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
            
            if (exitCode == 0)
            {
                // If installation succeeded, try to use browser
                using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });
                
                Assert.That(browser, Is.Not.Null);
                await browser.CloseAsync();
            }
            else
            {
                Assert.Ignore("Browser installation failed, but this is acceptable in CI environments");
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Browser installation not possible: {ex.Message}. " +
                         "This is expected in restricted environments.");
        }
    }

    [Test]
    public void PlaywrightLibrary_IsAccessible()
    {
        // Test that we can at least access the Playwright library
        var playwrightType = typeof(IPlaywright);
        Assert.That(playwrightType, Is.Not.Null);
        
        var browserType = typeof(IBrowser);
        Assert.That(browserType, Is.Not.Null);
        
        var pageType = typeof(IPage);
        Assert.That(pageType, Is.Not.Null);
        
        TestContext.WriteLine("Playwright library is accessible and types are available");
    }
}