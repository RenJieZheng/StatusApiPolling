using Microsoft.AspNetCore.Mvc;
using Status.Api.Services;

namespace Status.Api.Controllers;

[Route("/")]
[ApiController]
public class JobsController : ControllerBase
{
    private readonly IJobService  _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost("job/{duration?}/{fail?}")]
    public IActionResult StartJob(int? duration = null, bool fail = false)
    {   
        if (duration is null)
        {
            _jobService.StartJob();
            return Created("job", new { result = "created" });
        }
        else
        {
            _jobService.StartJob((int)duration, fail);
            return Created("job", new { result = "created" });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        string status;
        try {
            status = _jobService.GetStatus();
        }
        catch (InvalidOperationException ex) {
            return NotFound(ex.Message);
        }

        return Ok(new { result = status });
    }
}