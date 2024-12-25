using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using Status.Client;
using System.Text;
using System.Net;

namespace Status.IntegrationTests;

public class BasicTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_JobReturnCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/job", jsonContent);

        // 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_StatusReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/job", jsonContent);
        var response = await client.GetAsync("/status");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
    }

    [Fact]
    public async Task Get_StatusInitiallyReturnsPending() 
    {
        // Arrange
        var client = _factory.CreateClient();
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/job/1000/false", jsonContent);
        var response = await client.GetAsync("/status");

        // Assert
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JobStatus>(jsonResponse);
        Assert.NotNull(status);
        Assert.Equal("pending", status.result);
    }

    [Theory]
    [InlineData(false, "completed")]
    [InlineData(true, "error")]
    public async Task Get_StatusEventuallyReturnsFinalJobState(bool jobFails, string finalStatus) 
    {
        // Arrange
        var client = _factory.CreateClient();
        using StringContent jsonContent = new("", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync($"/job/1000/{jobFails}", jsonContent);
        var response = await client.GetAsync("/status");
        Thread.Sleep(1100);
        var response2 = await client.GetAsync("/status");

        // Assert
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JobStatus>(jsonResponse);
        Assert.NotNull(status);
        Assert.Equal("pending", status.result);

        var jsonResponse2 = await response2.Content.ReadAsStringAsync();
        var status2 = JsonSerializer.Deserialize<JobStatus>(jsonResponse2);
        Assert.NotNull(status2);
        Assert.Equal(finalStatus, status2.result);
    }
}
