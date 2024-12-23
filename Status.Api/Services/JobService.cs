using System;

namespace Status.Api.Services;

public interface IJobService
{
    void StartJob();
    bool JobStarted();
    string GetStatus();
}

public class JobService : IJobService
{
    private DateTime? _startTime;
    private double _jobDuration;
    private bool _jobFails;

    public JobService(IConfiguration configuration)
    {
        _jobDuration = configuration.GetValue<double>("JobSettings:JobDuration", 5000);
        _jobFails = configuration.GetValue<bool>("JobSettings:JobWillFail", false);
    }

    public void StartJob() {
        _startTime = DateTime.Now;
    }

    public bool JobStarted()
    {
        return _startTime is not null;
    }

    public string GetStatus()
    {
        if (_startTime is null) {
            throw new InvalidOperationException("Job was not started");
        }

        string status = "pending";
        if (((TimeSpan)(DateTime.Now - _startTime)).TotalMilliseconds > _jobDuration) 
        {
            if (_jobFails) {
                status = "error";
            }
            else
            {
                status = "completed";
            }
        }

        return status;
    }
}