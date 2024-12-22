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

// Api Endpoints
app.MapPost("/job", () => {
    jobStartTime = DateTime.Now;
    return Results.Created("job", new { result = "created"});
})
.WithName("PostJob")
.WithOpenApi();

app.MapGet("/status", () => {
    if (jobStartTime is null) {
        return Results.BadRequest(new { error = "Job has not started yet." });
    }

    TimeSpan elapsed = DateTime.Now - (DateTime)jobStartTime;
    double seconds = elapsed.TotalSeconds;

    var status = "pending";
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

    return Results.Ok(new { result = status });
})
.WithName("GetStatus")
.WithOpenApi();

app.Run();

public partial class Program { }