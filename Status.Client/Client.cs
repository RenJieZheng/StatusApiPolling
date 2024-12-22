using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Formats.Asn1;

namespace Status.Client;

public interface IStatusClient<T>
{
    Task<T> GetStatusAsync();
    Task<T> PollUntilCompleted();
}

public class StatusClient : IStatusClient<JobStatus>
{
    private readonly HttpClient _httpClient;
    private int _basePollRate;
    private double _exponentialBackoff;
    private int _timeOut;

    public StatusClient(HttpClient httpClient, int basePollRate, double exponentialBackoff, int timeOut)
    {
        if (basePollRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePollRate), "Base poll rate must be greater than 0");
        }
        if (exponentialBackoff <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentialBackoff), "Exponential backoff must be greater than 1");
        }
        if (timeOut <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeOut), "Timeout must be greater than 0");
        }

        _httpClient = httpClient;
        _basePollRate = basePollRate;
        _exponentialBackoff = exponentialBackoff;
        _timeOut = timeOut;
    }
    
    public async Task<JobStatus> GetStatusAsync() {
        using HttpResponseMessage response = await _httpClient.GetAsync("/status");
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();

        JobStatus? status;
        try 
        {
            status = JsonSerializer.Deserialize<JobStatus>(jsonResponse);
        } 
        catch (Exception ex)
        {
            if (ex is JsonException || ex is NotSupportedException || ex is ArgumentNullException)
            {
                throw new ApplicationException("Failed to deserialize the response from the server.", ex);
            }
            else
            {
                throw;
            }
        }

        if (status is null) {
            throw new ApplicationException("The server returned an invalid response (empty/null)");
        }
        
        return status;
    }

    public async Task<JobStatus> PollUntilCompleted() {
        double pollRate = _basePollRate;
        JobStatus? status;
        DateTime startTime = DateTime.Now;
        do {
            var response = await _httpClient.GetAsync("/status");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            try 
            {
                status = JsonSerializer.Deserialize<JobStatus>(jsonResponse);
            } 
            catch (Exception ex)
            {
                if (ex is JsonException || ex is NotSupportedException || ex is ArgumentNullException)
                {
                    throw new ApplicationException("Failed to deserialize the response from the server.", ex);
                }
                else
                {
                    throw;
                }
            }

            if (status is null || status.result == "pending") 
            {
                Thread.Sleep((int)pollRate);
                pollRate *= _exponentialBackoff;
            } else {
                break;
            }
        } while ((DateTime.Now - startTime).TotalMilliseconds < _timeOut);

        if (status is null)
        {
            throw new ApplicationException("Received inavlid response while polling for completion status (empty/null)");
        }
        else if (status.result == "pending") 
        {
            throw new TimeoutException("Polling operation timed out while waiting for completion status");
        } 
        else 
        {
            return status;
        }
    } 
}

public record class JobStatus {
    public string? result { get; set; }
}
