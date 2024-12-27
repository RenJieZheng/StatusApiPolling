using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using Status.Client;

namespace Status.Tests;

public class ClientApiIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ILogger<StatusClient>? _logger;
    private readonly IWaitTimeScheduler _defaultWaitTimeScheduler;
    private readonly IPollIntervalScheduler _defaultPollIntervalScheduler;

    public ClientApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _logger = null;
        _defaultWaitTimeScheduler = new AverageJobDurationScheduler();
        _defaultPollIntervalScheduler = new ConstantPollIntervalScheduler(20, 15);
    }   

    [Theory]
    [InlineData(false, "pending", "pending", "pending", "completed")]
    [InlineData(true, "pending", "pending", "pending", "error")]
    public async Task StatusClient_GetStatusReturnsCorrectStatus(bool jobFails, string status1, string status2, string status3, string finalStatus)
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);

        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/1000/{jobFails}", jsonContent);

        // Act
        var response1 = await statusClient.GetStatusAsync();
        await Task.Delay(400);
        var response2 = await statusClient.GetStatusAsync();
        await Task.Delay(400);
        var response3 = await statusClient.GetStatusAsync();
        await Task.Delay(210);
        var response4 = await statusClient.GetStatusAsync();

        // Assert
        Assert.Equal(status1, response1.result);
        Assert.Equal(status2, response2.result);
        Assert.Equal(status3, response3.result);
        Assert.Equal(finalStatus, response4.result);
    }

    [Theory]
    [InlineData(200, false, "completed")]
    [InlineData(200, true, "error")]
    public async Task StatusClient_PollStatusUntilCompletedAsync(int jobDuration, bool jobFails, string finalStatus)
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);

        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollUntilCompletedAsync();

        // Assert
        Assert.Equal(finalStatus, response.result);
    }

    [Fact]
    public async Task StatusClient_PollUntilCompletedAsync_ThrowsTimeoutException()
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);

        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/500/false", jsonContent);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () => 
            await statusClient.PollUntilCompletedAsync());
    }

     [Theory]
    [InlineData(200, false, "completed")]
    [InlineData(200, true, "error")]
    public async Task StatusClient_PollWithInitialWaitTimeAsync(int jobDuration, bool jobFails,
        string expectedFinalStatus)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);

        using var jsonContent = new StringContent("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollWithInitialWaitTimeAsync();

        // Assert
        Assert.Equal(expectedFinalStatus, response.result);
    }

    // ------------- Cancellation token ----------------------------------------
    
    [Fact]
    public async Task StatusClient_GetStatusAsync_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);
        cts.Cancel();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ApplicationException>(async () =>
            await statusClient.GetStatusAsync(cts.Token));
    }

    [Fact]
    public async Task StatusClient_PollUntilCompletedAsync_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await statusClient.PollUntilCompletedAsync(cts.Token));
    }

    [Fact]
    public async Task StatusClient_PollWithInitialWaitTimeAsync_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, _logger, _defaultWaitTimeScheduler, _defaultPollIntervalScheduler);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(async () =>
            await statusClient.PollWithInitialWaitTimeAsync(cts.Token));
    }
}