using System.Net;
using NUnit.Framework;

namespace TemenosAlertManager.Tests.Playwright.Tests.Basic;

[TestFixture]
public class BasicHttpTests
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _client = new HttpClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task HttpClient_IsWorking()
    {
        // Simple test to verify HTTP client functionality
        var response = await _client.GetAsync("https://httpbin.org/get");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HttpClient_CanHandleJson()
    {
        var response = await _client.GetAsync("https://httpbin.org/json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("slideshow"));
    }

    [Test]
    public void TestFramework_IsWorking()
    {
        // Basic test to verify NUnit is working
        Assert.That(true, Is.True);
        Assert.That("Hello", Is.EqualTo("Hello"));
        Assert.That(42, Is.EqualTo(42));
    }

    [Test]
    public void AsyncTest_IsSupported()
    {
        // Verify async test support
        var task = Task.FromResult(42);
        Assert.That(task.Result, Is.EqualTo(42));
    }
}