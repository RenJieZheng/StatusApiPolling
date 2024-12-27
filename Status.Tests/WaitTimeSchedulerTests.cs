using Status.Client;

namespace Status.Tests;

public class AverageJobDurationSchedulerTests
{
    [Fact]
    public void Constructor_ValidDefaultWaitTime_DoesNotThrow()
    {
        // Act & Assert
        var exception1 = Record.Exception(() => new AverageJobDurationScheduler(1, 1, 0.99));
        var exception2 = Record.Exception(() => new AverageJobDurationScheduler(10, 10, 0.01));
        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Theory]
    [InlineData(0, 1, 0.8)]
    [InlineData(-1, 1, 0.8)]
    [InlineData(1, 0, 0.8)]
    [InlineData(1, -1, 0.8)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -0.01)]
    [InlineData(1, 1, 1)]
    [InlineData(1, 1, 1.01)]
    public void Constructor_InvalidArguments_ThrowsArgumentOutOfRangeException(int defaultWaitTime, int numJobsRemembered, double overshootCorrection)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AverageJobDurationScheduler(defaultWaitTime, numJobsRemembered, overshootCorrection));
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
        var scheduler = new AverageJobDurationScheduler(10, 3, 0.8);

        // Act
        scheduler.UpdateFromResult(true, 100);
        scheduler.UpdateFromResult(true, 200);
        scheduler.UpdateFromResult(true, 300);
        var t1 = scheduler.GetWaitTime();

        scheduler.UpdateFromResult(true, 500);
        scheduler.UpdateFromResult(true, 600);
        scheduler.UpdateFromResult(true, 700);
        var t2 = scheduler.GetWaitTime();

        // Assert
        Assert.Equal(160, t1);
        Assert.Equal(480, t2);
    }
}