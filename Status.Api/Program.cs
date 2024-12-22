using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure job duration using the JOB_DURATION environment variable
double jobDuration = 10;
string? envJobDuration = builder.Configuration["JOB_DURATION"];
if (!string.IsNullOrWhiteSpace(envJobDuration) && double.TryParse(envJobDuration, out double duration)) 
{
    jobDuration = duration;
}
DateTime? jobStartTime = null;

// Configure if the job will fail (error status) using the JOB_WILL_FAIL environment variable
bool jobFails = false;
string? envJobFails = builder.Configuration["JOB_WILL_FAIL"];
if (!string.IsNullOrWhiteSpace(envJobFails) && bool.TryParse(envJobFails, out bool fail))
{
    jobFails = fail;
}
var status = "pending";

// Api Endpoints
app.MapGet("/status", () => {
    // Set the start time of a job to the first time the /status endpoint is hit
    if (jobStartTime is null) {
        jobStartTime = DateTime.Now;
    }

    TimeSpan elapsed = DateTime.Now - (DateTime)jobStartTime;
    double seconds = elapsed.TotalSeconds;

    if (seconds > jobDuration) 
    {
        if (jobFails)
        {
            status = "error";
        }
        else
        {
            status = "completed";
        }
    }

    return new {result = status};
})
.WithName("GetStatus")
.WithOpenApi();

app.Run();
