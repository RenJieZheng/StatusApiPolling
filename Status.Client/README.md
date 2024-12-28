# Status.Client Library Documentation

The **Status.Client** library provides utilities to retrieve and poll for job statuses from **Status.Api**.

[Full API XML Documentation](./docs/Status.Client.xml)

## Interfaces and Classes

#### IStatusClient\<T>:
Defines a client for retrieving and polling job statuses from the status API, where T is the type representing the returned job status.

Methods:
1. **`GetStatusAsync(CancellationToken ct)`**  
   Retrieves the current job status from the server.
2. **`PollUntilCompletedAsync(CancellationToken ct)`**  
   Repeatedly calls `GetStatusAsync` until the job is no longer pending or a maximum attempt threshold is reached.
3. **`PollUntilCompletedAsync(IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct)`**  
   Same as above, but uses a custom poll interval scheduler.
4. **`PollWithInitialWaitTimeAsync(CancellationToken ct)`**  
   Waits for an initial period (defined by a wait-time scheduler) before starting to poll. This may be used when job has just started and the user wants to be notified when the job finishes as soon as possible, and they already have an idea of how long the job is going to take.
5. **`PollWithInitialWaitTimeAsync(IWaitTimeScheduler waitTimeScheduler, IPollIntervalScheduler pollIntervalScheduler, CancellationToken ct)`**  
   Same as above, but uses a custom wait-time scheduler and poll interval scheduler before and during polling.

#### IWaitTimeScheduler
Determines an initial wait period (in milliseconds) before polling for the status. If a user wants to start polling right when a job starts, they can use the wait time to reduce unnessecary pollys.

Methods:
1. **`GetWaitTime()`**  
    Returns the recommended wait time in milliseconds.
2. **`UpdateFromResult(bool intialWaitSuccess, int jobDurationEstimate)`**  
    Adjusts future wait times based the result of the last poll.

#### IPollIntervalScheduler
Provide an iterator method `PollIntervals()` that yields interavls (in milliseconds) to wait between polls. A smart sheduler can reduce the number of polls required to figure out when a job is complete.

#### JobStatus
Represents the current status of a job, stored in the `result` field.

#### StatusClient
An implementation of `IStatusClient<T>` which works with `JobStatus` objects to store the status of a job. 
  ```csharp
  public StatusClient(
      HttpClient httpClient, 
      ILogger<StatusClient>? logger, 
      IWaitTimeScheduler? waitTimeScheduler,
      IPollIntervalScheduler? pollIntervalScheduler
  )
  ```
Uses an `httpClient` configured with the Status API's endpoint, an optional logger for diagnostics, and optional preconfigured `IWaitTimeScheduler` and `IPollIntervalScheduler`.

#### AverageJobDurationScheduler
An implementation of `IWaitTimeScheduler`.
```csharp
public AverageJobDurationScheduler(
    int defaultWaitTime = 10,
    int numJobsRemembered = 10,
    double overshootCorrection = 0.8
)
```
Returns a wait time based on the average of `numJobsRemembered` previous jobs. This average is multiplied by `overshootCorrection`. This accounts for two things:
1. Using the exact average means half of the jobs are expected to finish before the wait time. If the use does not want to have extra latency for half of jobs, they can adjust the correction so that more jobs will finish during the polling period.
2. If all jobs finish before the current polling interval, then the average will remain constant. Multiplying by the correction prevents this.

This scheduler would be useful if the duration of a job does not vary greatly, and thus the average of previous jobs gives a give estimate of how long a job is going to take. 

In the context of video translation, the scheduler may need to be more complicated and include some kind of `durationPerMinute` estimation. 


#### ConstantPollIntervalScheduler
An implementation of `IPollIntervalScheduler`.
```csharp
public ConstantPollIntervalScheduler(int pollInterval = 10, int maxPollAttempts = 15)
```
The intervals are constant and set by the `pollInterval` parameter.

#### ExponentialBackoffScheduler
An implementation of `IPollIntervalScheduler`.
```csharp
public ExponentialBackoffScheduler(
    int basePollInterval = 10, 
    double exponentialBackoff = 2, 
    int maxPollAttempts = 15
)
```
Starting with a poll interval of `basePollInterval`, and subsqeunt poll attempts will update the interval exponentially by multiplying by `exponentialBackoff`. The advantage of this strategy is the the number of calls is $O(\log(T))$ where $T$ is the amount time the job took to finish starting from the very first poll. 

## Usage Examples

#### 1. Simple call to GetStatusAsyn
```csharp
// Initialize ahead of time an httpClient to point to the status api, logger, and schedulers

IStatusClient<JobStatus> client = new StatusClient(
    httpClient, 
    logger, 
    wtScheduler, 
    piScheduler
);

var status = await client.GetStatusAsync();
```

#### 2. Polling until the job completes (completed or error) using default poll interval scheduler
```csharp
var finalStatus = await client.PollUntilCompletedAsync();
```

#### 3. Polling until the job completes (completed or error) using custom scheduler 
```csharp
var exponentialScheduler = new ExponentialBackoffScheduler(
    basePollInterval: 10, 
    exponentialBackoff: 2.0, 
    maxPollAttempts: 15
);

var finalStatus = await client.PollUntilCompletedAsync(exponentialScheduler);
```

#### 4. Using an Initial Wait Time
```csharp
var finalStatus = await client.PollWithInitialWaitTimeAsync();
```

#### 5. Using an Initial Wait Time and custome schedulers
```csharp
var waitTimeScheduler = new AverageJobDurationScheduler(
    defaultWaitTime: 10, 
    numJobsRemembered: 5, 
    overshootCorrection: 0.9
);

var pollIntervalScheduler = new ConstantPollIntervalScheduler(
    pollInterval: 50, 
    maxPollAttempts: 20
);

var finalStatus = await client.PollWithInitialWaitTimeAsync();
```