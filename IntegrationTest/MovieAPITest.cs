using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Test;

public class MovieApiTests
{
    private HttpClient? _client;
    
    [SetUp]
    public void Setup()
    {
        var webApplicationFactory = new WebApplicationFactory<Program>();
        _client = webApplicationFactory.CreateClient(); 
    }

    [Test]
    public async Task Test_that_root_endpoints_shows_correct_data()
    {
        
        // Act
        if (_client == null)
        {
            Assert.Fail("Client is null");
            return;
        }
        
        var response = await _client.GetAsync("/");
        var stringAsync = await response.Content.ReadAsStringAsync();
        Assert.That(stringAsync, Is.EqualTo("Welcome to MovieProxy"));
    }
}