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

    [Theory]
    [InlineData(false, "pending", "pending", "pending", "completed")]
    [InlineData(true, "pending", "pending", "pending", "error")]
    public async Task StatusClient_GetStatusReturnsCorrectStatus(bool jobFails, string status1, string status2, string status3, string finalStatus)
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

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
    [InlineData(500, false, "completed")]
    [InlineData(500, true, "error")]
    public async Task StatusClient_PollStatusUntilCompletedAsync(int jobDuration, bool jobFails, string finalStatus)
    {
        // Arrange
        var client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

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
        StatusClient statusClient = new StatusClient(client, 10, 2, 3);

        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/500/false", jsonContent);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () => 
            await statusClient.PollUntilCompletedAsync());
    }

     [Theory]
    [InlineData(500, false, "completed")]
    [InlineData(500, true, "error")]
    public async Task StatusClient_PollWithInitialWaitTimeAsync(int jobDuration, bool jobFails,
        string expectedFinalStatus)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

        using var jsonContent = new StringContent("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollWithInitialWaitTimeAsync(400);

        // Assert
        Assert.Equal(expectedFinalStatus, response.result);
    }

    [Theory]
    [InlineData(200, false, "completed")]
    [InlineData(200, true, "error")]
    public async Task StatusClient_PollWithConstantIntervalAsync_Default(int jobDuration, bool jobFails, string expectedFinalStatus)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 50, 2, 15);

        using var jsonContent = new StringContent("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollWithConstantIntervalAsync();

        // Assert
        Assert.Equal(expectedFinalStatus, response.result);
    }

    [Theory]
    [InlineData(200, false, "completed", 50)]
    [InlineData(200, true, "error", 100)]
    public async Task StatusClient_PollWithConstantIntervalAsync_Custom(int jobDuration, bool jobFails, string expectedFinalStatus, int rate)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

        using var jsonContent = new StringContent("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollWithConstantIntervalAsync(rate, default);

        // Assert
        Assert.Equal(expectedFinalStatus, response.result);
    }

    [Theory]
    [InlineData(50, 2.0, 15, 500, false, "completed")]
    [InlineData(100, 1.5, 15, 500, true, "error")]
    public async Task StatusClient_PollWithExponentialBackoffAsync(int rate, 
        double exponentialBackoff, 
        int maxAttempts, 
        int jobDuration,
        bool jobFails,
        string expectedFinalStatus)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

        using var jsonContent = new StringContent("", Encoding.UTF8, "application/json");
        await client.PostAsync($"/job/{jobDuration}/{jobFails}", jsonContent);

        // Act
        var response = await statusClient.PollWithExponentialBackoffAsync(rate, exponentialBackoff, maxAttempts);

        // Assert
        Assert.Equal(expectedFinalStatus, response.result);
    }

    // --------------- Argument exceptions ------------------------------------------------------------------------
    [Theory]
    [InlineData(0, 2, 5)]
    [InlineData(-1, 2, 5)]
    [InlineData(10, 0.9, 5)]
    [InlineData(10, 0, 5)]
    [InlineData(10, -1, 5)]
    [InlineData(10, 2, 0)]
    [InlineData(10, 2, -1)]
    public void StatusClient_ArgumentOutOfRangeException(int rate, double exponentialBackoff, int maxAttempts)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();

        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StatusClient(client, rate, exponentialBackoff, maxAttempts));
    }

    // Argument exceptions
    [Theory]
    [InlineData(0, 2, 5)]
    [InlineData(-1, 2, 5)]
    [InlineData(10, 0.9, 5)]
    [InlineData(10, 0, 5)]
    [InlineData(10, -1, 5)]
    [InlineData(10, 2, 0)]
    [InlineData(10, 2, -1)]
    public async Task StatusClient_PollWithExponentialBackoffAsync_ArgumentOutOfRangeException(int rate, double exponentialBackoff, int maxAttempts)
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        StatusClient statusClient = new StatusClient(client, 10, 2, 15);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            statusClient.PollWithExponentialBackoffAsync(rate, exponentialBackoff, maxAttempts));
    }

    // ------------- Cancellation token ----------------------------------------
    
    [Fact]
    public async Task StatusClient_GetStatusAsync_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, 10, 2, 15);
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
        var statusClient = new StatusClient(client, 10, 2, 15);
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
        var statusClient = new StatusClient(client, 10, 2, 15);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await statusClient.PollWithInitialWaitTimeAsync(200, cts.Token));
    }

    [Fact]
    public async Task StatusClient_PollWithConstantIntervalAsyncDefault_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, 10, 2, 15);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await statusClient.PollWithConstantIntervalAsync(cts.Token));
    }

    [Fact]
    public async Task StatusClient_PollWithConstantIntervalAsyncCustom_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, 10, 2, 15);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await statusClient.PollWithConstantIntervalAsync(200, cts.Token));
    }

    [Fact]
    public async Task StatusClient_PollWithExponentialBackoffAsync_Cancels()
    {
         // Arrange
        var cts = new CancellationTokenSource();
        HttpClient client = _factory.CreateClient();
        var statusClient = new StatusClient(client, 10, 2, 15);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await statusClient.PollWithExponentialBackoffAsync(10, 2, 15, cts.Token));
    }
}