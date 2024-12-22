using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

double jobDuration = builder.Configuration.GetValue<double>("JobSettings:JobDuration", 5);
bool jobFails = builder.Configuration.GetValue<bool>("JobSettings:JobWillFail", false);

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


DateTime? jobStartTime = null;
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

public partial class Program { }