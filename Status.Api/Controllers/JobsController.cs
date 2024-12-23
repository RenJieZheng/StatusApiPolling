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

    [HttpPost("job")]
    public IActionResult StartJob()
    {
        _jobService.StartJob();
        return Created("job", new { result = "created" });
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