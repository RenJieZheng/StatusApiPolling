using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace Status.Client;

public interface IStatusClient<T>
{
    Task<T> GetStatusAsync(CancellationToken ct = default);
    Task<T> PollUntilCompleted(CancellationToken ct = default);
    Task<T> PollWithInitialWaitTime(int initialWaitTime, CancellationToken ct = default);
    Task<T> PollWithConstantInterval(int rate, CancellationToken ct = default);
    Task<T> PollWithExponentialBackoff(int rate, double exponentialBackoff, CancellationToken ct = default);
}

public class StatusClient : IStatusClient<JobStatus>
{
    private readonly HttpClient _httpClient;
    private int _basePollRate;
    private double _exponentialBackoff;
    private int _maxPollAttempts;

    public StatusClient(HttpClient httpClient, int basePollRate = 10, double exponentialBackoff = 2, int maxPollAttempts = 25)
    {
        if (basePollRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePollRate), "Base poll rate must be greater than 0");
        }
        if (exponentialBackoff <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentialBackoff), "Exponential backoff must be greater than 1");
        }
        if (maxPollAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPollAttempts), " Maximum number of poll attempts must be greater than 0");
        }

        _httpClient = httpClient;
        _basePollRate = basePollRate;
        _exponentialBackoff = exponentialBackoff;
        _maxPollAttempts = maxPollAttempts;
    }
    
    public async Task<JobStatus> GetStatusAsync(CancellationToken ct = default) {
        var status = await _httpClient.GetFromJsonAsync<JobStatus>("/status", ct);
        if (status is null)
        {
            throw new ApplicationException("The server returned an invalid response (empty/null).");
        }
        return status;
    }

    public async Task<JobStatus> PollUntilCompleted(CancellationToken ct = default) {
        return await PollWithExponentialBackoff(_basePollRate, _exponentialBackoff, ct);
    } 

    public async Task<JobStatus> PollWithInitialWaitTime(int initialWaitTime, CancellationToken ct = default) {
        await Task.Delay(initialWaitTime);
        return await PollUntilCompleted(ct);
    }
    
    public async Task<JobStatus> PollWithConstantInterval(int rate, CancellationToken ct) {
        return await PollWithExponentialBackoff(rate, 1.0, ct);
    }

    public async Task<JobStatus> PollWithExponentialBackoff(int rate, double exponentialBackoff, CancellationToken ct = default) {
        double pollRate = rate;
        JobStatus? status = null;

        for (int i = 0; i < _maxPollAttempts; i += 1) 
        {
            ct.ThrowIfCancellationRequested();
            status = await GetStatusAsync(ct);
            if (status is null || status.result == "pending") 
            {
                await Task.Delay((int)pollRate);
                pollRate *= exponentialBackoff;
            } 
            else 
            {
                break;
            }
        } 

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
