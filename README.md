# StatusApiPolling

My solution for a take home assessment from HeyGen (https://www.heygen.com/).

The prompt was to create a simple API that has an endpoint returing the status of a job, and a client library that provides utilities to poll for the status of the job.

Project structure:  
- `Status.Api` – The Status API project.  
- `Status.Client` – A library that provides a client to interact with the Status API.  
- `Status.Tests` – Unit and integration tests for both the API and client.

## Prerequisites

- `.NET 8` installed.

## Building the Solution

1. **Clone the repo**:
   ```bash
   git clone <link to this repository>
   cd StatusApiPolling
   ```

2. **Restore NuGet packages and build the entire solution**:
    ```bash
    dotnet restore
    dotnet build
    ```

## Running the API

1. **Using the CLI**:
    ```bash
    dotnet run --project Status.Api --launch-profile "http"
    ```
2. **To launch with Swagger UI**:
    ```bash
    dotnet watch run --project Status.Api --launch-profile "http"
    ```
Check `./Status.Api/Properties.launchSettings.json` for other launch profiles.

### Endpoints

**POST** `/job/{duration?}/{fail?}`  
- Starts a job.  
- `duration` (int? | optional) - How long the job should run.  
- `fail` (bool | optional) - Whether to simulate a failed job.  
- Returns `201 Created` and a JSON result.

**GET** `/status`  
- Retrieves the current job status.  
- Returns `200 OK` and a JSON result if a job is found, or `404 Not Found` otherwise.

### Configuration options
You can modify default values for jobs by editing the `JobSettings` section in `appsettings.json`.  
- `JobDuration`: The default duration (in milliseconds) for a job.  
- `JobWillFail`: Whether the job is set to fail by default (true/false).  

These will be the settings for jobs starting by the `/job` endpoint with no parameters provided.

## Client Library
[Library Documentation](./Status.Client/README.md)

## Tests
Run all tests with:
```bash
dotnet test
```

### Api Tests
Some basic tests of the API can be found in `./Status.Tests/BasicApiTests.cs`.

### Wait Time and Polling Interval Scheduler Tests
Unit tests for the classes implementing these interfaces can be found in `./Status.Tests/WaitTimeSchedulerTests.cs` and `./Status.Tests/PollingIntervalSchedulerTests.cs` respectively.

### Client and API Integration tests
Located in `./Status.Tests/ClientApiIntegrationTests.cs`.

Two of the tests will outputs logs, these can be run individually to view the logs in the console with:
```bash
dotnet test --filter "FullyQualifiedName~PollStatusUntilCompletedAsyncDemonstrateLogging" --logger "console;verbosity=normal"
dotnet test --filter "FullyQualifiedName~PollWithInitialWaitTimeAsyncDemonstrateLogging" --logger "console;verbosity=normal"
```