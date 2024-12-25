using System;

namespace Status.Api.Services;

public interface IJobService
{
    void StartJob();
    void StartJob(int duration, bool fails);
    bool JobStarted();
    string GetStatus();
}

public class JobService : IJobService
{
    private DateTime? _startTime;
    private int _baseJobDuration;
    private bool _baseJobFailure;

    private int _jobDuration;
    private bool _jobFails;

    public JobService(IConfiguration configuration)
    {
        _baseJobDuration = configuration.GetValue<int>("JobSettings:JobDuration", 5000);
        _baseJobFailure = configuration.GetValue<bool>("JobSettings:JobWillFail", false);
    }

    public void StartJob() {
        _startTime = DateTime.Now;
        _jobDuration = _baseJobDuration;
        _jobFails = _baseJobFailure;
    }

    public void StartJob(int duration, bool fails) 
    {
        _startTime = DateTime.Now;
        _jobDuration = duration;
        _jobFails = fails; 
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