using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollUntilCompletedAsync(CancellationToken ct = default);

    /// <summary>
    /// Continuously polls the server until the job is no longer pending or the maximum attempts are reached.
    /// </summary>
    /// <param name="pollIntervalScheduler">Scheduler for generating polling intervals.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollUntilCompletedAsync(IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct = default);

    /// <summary>
    /// Waits for an initial period before starting to poll the server for job status.
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithInitialWaitTimeAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits for an initial period before starting to poll the server for job status.
    /// </summary>
    /// <param name="waitTimeScheduler">Schduler for generating the initial wait time.</param>
    /// <param name="pollIntervalScheduler">Scheduler for generating polling intervals.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The final <see cref="JobStatus"/>.</returns>
    Task<T> PollWithInitialWaitTimeAsync(IWaitTimeScheduler waitTimeScheduler, IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct = default);
}   

/// <summary>
/// Implements a client for retrieving and polling job statuses from the status API.
/// </summary>
public class StatusClient : IStatusClient<JobStatus>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly IWaitTimeScheduler? _waitTimeScheduler;
    private readonly IPollIntervalScheduler? _pollIntervalScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to make requests to the status API.</param>
    /// <param name="logger">Logger to log information and warnings</param>
    /// <param name="waitTimeScheduler">Schduler for generating an initial wait time.</param>
    /// <param name="pollIntervalScheduler">Scheduler for generating polling intervals.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
    public StatusClient(
        HttpClient httpClient, 
        ILogger<StatusClient>? logger = null,
        IWaitTimeScheduler? waitTimeScheduler = null,
        IPollIntervalScheduler? pollIntervalScheduler = null
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
        _waitTimeScheduler = waitTimeScheduler;
        _pollIntervalScheduler = pollIntervalScheduler;
    }
    
    /// <inheritdoc/>
    public async Task<JobStatus> GetStatusAsync(CancellationToken ct = default) 
    {
        _logger?.LogInformation("Attempting to get the job status from the API.");

        JobStatus? status;

        try 
        {
            status = await _httpClient.GetFromJsonAsync<JobStatus>("/status", ct);
        }
        catch (Exception ex) 
        {
            _logger?.LogError(ex, "An error occurred while trying to get a response from the status API.");
            throw new ApplicationException("A problem occurred while trying to get a response from the status API: ", ex);
        }


        if (status is null)
        {
            _logger?.LogError("The server returned an invalid response (empty/null).");
            throw new ApplicationException("The server returned an invalid response (empty/null).");
        }

        _logger?.LogInformation("Successfully received job status from the API.");
        _logger?.LogDebug($"Returning job status: {status}");
        return status;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown when the clients poll interval scheduler is not configured.</exception>
    public async Task<JobStatus> PollUntilCompletedAsync(CancellationToken ct = default)
    {
        if (_pollIntervalScheduler is null)
        {
            _logger?.LogError("Status Client does not have poll interval scheduler configured.");
            throw new InvalidOperationException("Status Client does not have poll interval scheduler configured.");
        }
        return await Poll(_pollIntervalScheduler, ct);
    }

    /// <inheritdoc/>
    public async Task<JobStatus> PollUntilCompletedAsync(IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct = default)
    {
        return await Poll(pollIntervalScheduler, ct);
    }

    /// <inheritdoc/>
    /// /// <exception cref="InvalidOperationException">Thrown when the clients wait time or poll interval scheduler is not configured.</exception>
    public async Task<JobStatus> PollWithInitialWaitTimeAsync(CancellationToken ct = default)
    {
        if (_waitTimeScheduler is null)
        {
            _logger?.LogError("Status Client does not have wait time scheduler configured.");
            throw new InvalidOperationException("Status Client does not have wait time scheduler configured.");
        }
        if (_pollIntervalScheduler is null)
        {
            _logger?.LogError("Status Client does not have poll interval scheduler configured.");
            throw new InvalidOperationException("Status Client does not have poll interval scheduler configured.");
        }
        return await PollWithWaitTime(_waitTimeScheduler, _pollIntervalScheduler, ct);
    }

    /// <inheritdoc/>
    public async Task<JobStatus> PollWithInitialWaitTimeAsync(IWaitTimeScheduler waitTimeScheduler, IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct)
    {
        return await PollWithWaitTime(waitTimeScheduler, pollIntervalScheduler, ct);
    }


    // ------------ Private Methods ----------------------------------------------------------------

    private async Task<JobStatus> PollWithWaitTime(IWaitTimeScheduler waitTimeScheduler, IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct = default)
    {
        DateTime startTime = DateTime.Now;

        int wait = waitTimeScheduler.GetWaitTime();
        _logger?.LogInformation($"Waiting {wait} milliseconds");
        await Task.Delay(wait);

        JobStatus initialStatus = await GetStatusAsync(ct);
        if (initialStatus.result == "pending")
        {
            var status = await Poll(pollIntervalScheduler, ct);
            var elapsed = DateTime.Now - startTime;
            waitTimeScheduler.UpdateFromResult(false, (int)elapsed.TotalMilliseconds);
            return status;
        }
        else
        {
            _logger?.LogInformation("Initial poll after wait was successful");
            waitTimeScheduler.UpdateFromResult(true, waitTimeScheduler.GetWaitTime());
            return initialStatus;
        }
    }
    
    private async Task<JobStatus> Poll(IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct = default) 
    {
        _logger?.LogInformation("Begin polling until status changes from pending");

        JobStatus? status = null;

        foreach (int interval in pollIntervalScheduler.PollIntervals()) 
        {
            ct.ThrowIfCancellationRequested();
            status = await GetStatusAsync(ct);
            if (status is null || status.result == "pending") 
            {
                _logger?.LogDebug($"Current polling interval: {interval}");
                await Task.Delay(interval);
            } 
            else 
            {
                break;
            }
        } 

        if (status is null)
        {
            _logger?.LogError("Received inavlid response while polling for completion status (empty/null).");
            throw new ApplicationException("Received inavlid response while polling for completion status (empty/null).");
        }
        else if (status.result == "pending") 
        {
            _logger?.LogError("Polling operation reached maximum allowed attempts while polling for completion status.");
            throw new TimeoutException("Polling operation reached maximum allowed attempts while polling for completion status.");
        } 
        else 
        {
            _logger?.LogInformation("Polling completed");
            return status;
        }
    }
}

/// <summary>
/// Defines methods to calculate a wait time before the next poll and to log the result of a completed poll.
/// </summary>
public interface IWaitTimeScheduler
{
    /// <summary>
    /// Retrieves the current recommended wait time (in seconds) before the next polling attempt.
    /// </summary>
    /// <returns>An integer representing the wait time in seconds.</returns>
    public int GetWaitTime();

    /// <summary>
    /// Updates the initial wait time based on if the current wait time was sufficient
    /// and an estimate for how long the job took to complete.
    /// </summary>
    /// <param name="intialWaitSuccess">
    /// A boolean indicating if the last poll started successfully without being too early or too late.
    /// </param>
    /// <param name="jobDurationEstimate">
    /// An integer representing the job duration estimate (in milliseconds) for the last poll.
    /// </param>
    public void UpdateFromResult(bool intialWaitSuccess, int jobDurationEstimate);
}

/// <summary>
/// An implementation of <see cref="IWaitTimeScheduler"/> that computes an average
/// job duration to suggest an optimal wait time for future polls.
/// </summary>
public class AverageJobDurationScheduler : IWaitTimeScheduler
{
    private long _totalJobDuration;
    private int _pollCount;
    private int _defaultWaitTime;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageJobDurationScheduler"/> class.
    /// </summary>
    /// <param name="defaultWaitTime">The default wait time in milliseconds. Must be greater than 0. Default is 10ms.</param>\
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="defaultWaitTime"/> is invalid.</exception>
    public AverageJobDurationScheduler(int defaultWaitTime = 10)
    {
        if (defaultWaitTime <= 0) 
        {
            throw new ArgumentOutOfRangeException(nameof(defaultWaitTime), "Default wait time must be greater than 0");
        }
        _defaultWaitTime = defaultWaitTime;
    }

    /// <inheritdoc/>
    public int GetWaitTime()
    {
        if (_pollCount == 0)
        {
            return _defaultWaitTime;
        }

        return (int)(_totalJobDuration / _pollCount);
    }

    /// <inheritdoc/>
    public void UpdateFromResult(bool initialWaitSuccess, int jobDurationEstimate)
    {
        _pollCount++;
        _totalJobDuration += jobDurationEstimate;
    }
}

/// <summary>
/// Provides an iterator method that yields poll intervals.
/// </summary>
public interface IPollIntervalScheduler
{
    /// <summary>
    /// Iterator method that yields poll intervals. 
    /// </summary>
    public IEnumerable<int> PollIntervals();
}

/// <summary>
/// Poll interval scheduler that generates exponentially increasing poll intervals starting from a base rate, up to a maximum number of attempts.
/// </summary>
public class ExponentialBackoffScheduler : IPollIntervalScheduler
{
    private int _basePollInterval;
    private double _exponentialBackoff;
    private int _maxPollAttempts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffScheduler"/> class.
    /// </summary>
    /// <param name="basePollInterval">The base polling interval in milliseconds. Must be greater than 0. Default is 10ms.</param>
    /// <param name="exponentialBackoff">The multiplier for exponential backoff. Must be greater than 1. Default is 2.</param>
    /// <param name="maxPollAttempts">The maximum number of polling attempts. Must be greater than 0. Default is 15.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="basePollInterval"/>, <paramref name="exponentialBackoff"/>, or <paramref name="maxPollAttempts"/> are invalid.</exception>
    public ExponentialBackoffScheduler(
        int basePollInterval = 10, 
        double exponentialBackoff = 2, 
        int maxPollAttempts = 15
    )
    {
        if (basePollInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePollInterval), "Base poll interval must be greater than 0");
        }
        if (exponentialBackoff < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentialBackoff), "Exponential backoff must be greater than 1");
        }
        if (maxPollAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPollAttempts), " Maximum number of poll attempts must be greater than 0");
        }
        _basePollInterval = basePollInterval;
        _exponentialBackoff = exponentialBackoff;
        _maxPollAttempts = maxPollAttempts;
    }

    /// <summary>
    /// Iterator method that yields poll intervals. 
    /// Starts at base poll rate, and multiplies by exponentialBackoff for each subsequent attempt.
    /// </summary>
    public IEnumerable<int> PollIntervals()
    {
        double pollInterval = _basePollInterval;

        for (int attempt = 1; attempt <= _maxPollAttempts; attempt++)
        {
            yield return (int)pollInterval;
            pollInterval = pollInterval * _exponentialBackoff;
        }
    }
}

/// <summary>
/// Poll interval scheduler that generates constant poll intervals
/// </summary>
public class ConstantPollIntervalScheduler : ExponentialBackoffScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffScheduler"/> class.
    /// </summary>
    /// <param name="pollInterval">The polling interval in milliseconds. Must be greater than 0. Default is 10ms.</param>
    /// <param name="maxPollAttempts">The maximum number of polling attempts. Must be greater than 0. Default is 15.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pollInterval"/>, or <paramref name="maxPollAttempts"/> are invalid.</exception>
    public ConstantPollIntervalScheduler(int pollInterval = 10, int maxPollAttempts = 15) 
        : base(pollInterval, 1, maxPollAttempts)
    {}
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
