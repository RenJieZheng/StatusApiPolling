using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using Status.Client;

namespace Status.IntegrationTests;

public class ClientApiIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StatusClient_GetStatusReturnsStatus() 
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 100, 2, 30000);
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/job", jsonContent);
        var response1 = await statusClient.GetStatusAsync();
        Thread.Sleep(5100);
        var response2 = await statusClient.GetStatusAsync();

        // Assert
        Assert.Equal("pending", response1.result);
        Assert.Equal("completed", response2.result);
    }    

    [Fact]
    public async Task StatusClient_PollUntilCompletedReturnsCompleted()
    {
         // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 100, 2, 30000);
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/job", jsonContent);
        var response = await statusClient.PollUntilCompleted();

        // Assert
        Assert.Equal("completed", response.result);
    }

    [Fact]
    public async Task StatusClient_PollUntilCompletedTimesOut()
    {
         // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 100, 2, 4000);
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act and Assert
        await client.PostAsync("/job", jsonContent);
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            statusClient.PollUntilCompleted
        );
        Assert.Contains("Polling operation timed out", exception.Message);
    }
}