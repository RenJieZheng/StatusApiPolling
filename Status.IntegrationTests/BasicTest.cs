using Microsoft.AspNetCore.Mvc.Testing;

namespace Status.IntegrationTests;

public class BasicTest
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_StatusReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/status");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }
}