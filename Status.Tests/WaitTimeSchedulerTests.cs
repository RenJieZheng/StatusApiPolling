using Status.Client;

namespace Status.Tests;

public class AverageJobDurationSchedulerTests
{
    [Fact]
    public void Constructor_ValidDefaultWaitTime_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new AverageJobDurationScheduler(10));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_InvalidDefaultWaitTime_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new AverageJobDurationScheduler(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AverageJobDurationScheduler(-5));
    }

    [Fact]
    public void GetWaitTime_NoUpdates_ReturnsDefaultWaitTime()
    {
        // Arrange
        int defaultWaitTime = 10;
        var scheduler = new AverageJobDurationScheduler(defaultWaitTime);

        // Act
        int waitTime = scheduler.GetWaitTime();

        // Assert
        Assert.Equal(defaultWaitTime, waitTime);
    }

    [Fact]
    public void UpdateFromResult_UpdatesWaitTime_CorrectAverage()
    {
        // Arrange
        var scheduler = new AverageJobDurationScheduler();

        // Act
        scheduler.UpdateFromResult(true, 100);
        scheduler.UpdateFromResult(true, 200);
        scheduler.UpdateFromResult(true, 300);

        // Assert
        Assert.Equal(200, scheduler.GetWaitTime());
    }
}