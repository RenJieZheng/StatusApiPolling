using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace Status.Client;

/// <summary>
/// Defines a client for retrieving and polling job statuses from the status API.
/// </summary>
/// <typeparam name="T">The type representing the job status.</typeparam>
public interface IStatusClient<T>
{
    /// <summary>
    /// Retrieves the current job status from the server.
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The current <see cref="JobStatus"/>.</returns>
    Task<T> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Continuously polls the server until the job is no longer pending or the maximum attempts are reached.
    /// Uses exponential backoff to increase the amount of time between polls exponentially.
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollUntilCompletedAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits for an initial period before starting to poll the server for job status.
    /// Uses exponential backoff to increase the amount of time between polls exponentially after the initial wait period.
    /// </summary>
    /// <param name="initialWaitTime">Initial wait time in milliseconds.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithInitialWaitTimeAsync(int initialWaitTime, CancellationToken ct = default);

    /// <summary>
    /// Continuously polls the server at a constant interval until the job is no longer pending or the maximum attempts are reached.
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithConstantIntervalAsync(CancellationToken ct = default);

    /// <summary>
    /// Continuously polls the server at a constant interval until the job is no longer pending or the maximum attempts are reached.
    /// </summary>
    /// <param name="rate">Polling rate in milliseconds.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithConstantIntervalAsync(int rate, CancellationToken ct = default);

    /// <summary>
    /// Continuously polls the server with an exponential backoff strategy until the job is no longer pending or the maximum attempts are reached.
    /// </summary>
    /// <param name="rate">Initial polling rate in milliseconds.</param>
    /// <param name="exponentialBackoff">Multiplier for exponential backoff.</param>
    /// <param name="maxAttempts">The maximum number of polling attempts. Must be greater than 0. </param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithExponentialBackoffAsync(int rate, double exponentialBackoff, int maxAttempts, CancellationToken ct = default);
}

/// <summary>
/// Implements a client for retrieving and polling job statuses from the status API.
/// </summary>
public class StatusClient : IStatusClient<JobStatus>
{
    private readonly HttpClient _httpClient;
    private int _basePollRate;
    private double _exponentialBackoff;
    private int _maxPollAttempts;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to make requests to the status API.</param>
    /// <param name="basePollRate">The base polling rate in milliseconds. Must be greater than 0. Default is 10ms.</param>
    /// <param name="exponentialBackoff">The multiplier for exponential backoff. Must be greater than 1. Default is 2.</param>
    /// <param name="maxPollAttempts">The maximum number of polling attempts. Must be greater than 0. Default is 15.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="basePollRate"/>, <paramref name="exponentialBackoff"/>, or <paramref name="maxPollAttempts"/> are invalid.</exception>
    public StatusClient(HttpClient httpClient, int basePollRate = 10, double exponentialBackoff = 2, int maxPollAttempts = 15)
    {
        if (basePollRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePollRate), "Base poll rate must be greater than 0");
        }
        if (exponentialBackoff < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentialBackoff), "Exponential backoff must be greater than 1");
        }
        if (maxPollAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPollAttempts), " Maximum number of poll attempts must be greater than 0");
        }

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _basePollRate = basePollRate;
        _exponentialBackoff = exponentialBackoff;
        _maxPollAttempts = maxPollAttempts;
    }
    
    /// <inheritdoc/>
    public async Task<JobStatus> GetStatusAsync(CancellationToken ct = default) 
    {
        JobStatus? status;

        try 
        {
            status = await _httpClient.GetFromJsonAsync<JobStatus>("/status", ct);
        }
        catch (Exception ex) 
        {
            throw new ApplicationException("A problem occurred while trying to get a response from the status API: ", ex);
        }


        if (status is null)
        {
            throw new ApplicationException("The server returned an invalid response (empty/null).");
        }
        return status;
    }

    /// <inheritdoc/>
    public async Task<JobStatus> PollUntilCompletedAsync(CancellationToken ct = default) 
    {
        return await PollWithExponentialBackoffAsync(_basePollRate, _exponentialBackoff, _maxPollAttempts, ct);
    } 

    /// <inheritdoc/>
    public async Task<JobStatus> PollWithInitialWaitTimeAsync(int initialWaitTime, CancellationToken ct = default) 
    {
        await Task.Delay(initialWaitTime);
        return await PollUntilCompletedAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<JobStatus> PollWithConstantIntervalAsync(CancellationToken ct = default) 
    {
        return await PollWithExponentialBackoffAsync(_basePollRate, 1.0, _maxPollAttempts, ct);
    }
    
    /// <inheritdoc/>
    public async Task<JobStatus> PollWithConstantIntervalAsync(int rate, CancellationToken ct) 
    {
        return await PollWithExponentialBackoffAsync(rate, 1.0, _maxPollAttempts, ct);
    }

    /// <inheritdoc/>
    public async Task<JobStatus> PollWithExponentialBackoffAsync(int rate, double exponentialBackoff, int maxAttempts, CancellationToken ct = default) 
    {
        if (rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Poll rate must be greater than 0");
        }
        if (exponentialBackoff < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentialBackoff), "Exponential backoff must be greater than 1");
        }
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), " Maximum number of poll attempts must be greater than 0");
        }

        double pollRate = rate;
        JobStatus? status = null;

        for (int i = 0; i < maxAttempts; i += 1) 
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
            throw new TimeoutException("Polling operation reached maximum allowed attempts while polling for completion status");
        } 
        else 
        {
            return status;
        }
    }
}

/// <summary>
/// Represents the status of a job in the system.
/// </summary>
public record class JobStatus 
{
    /// <summary>
    /// The result of the job. Can be "pending", "completed", or "error".
    /// </summary>
    public string? result { get; set; }
}
