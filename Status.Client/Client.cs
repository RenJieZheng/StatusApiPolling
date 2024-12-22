using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Status.Client;

public interface IStatusClient<T>
{
    Task<T> GetStatusAsync();
}

public class StatusClient : IStatusClient<JobStatus>
{
    private readonly HttpClient _httpClient;

    public StatusClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<JobStatus> GetStatusAsync() {
        using HttpResponseMessage response = await _httpClient.GetAsync("/status");
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JobStatus>(jsonResponse);
        
        return status ?? new JobStatus{ Result = "No response found" };
    }
}

public record class JobStatus {
    public string? Result { get; set; }
}
