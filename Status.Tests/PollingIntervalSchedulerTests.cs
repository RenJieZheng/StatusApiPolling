using Status.Client;

namespace Status.Tests;

public class ExponentialBackoffSchedulerTests
{
    [Theory]
    [InlineData(10, 2, 15)]  
    [InlineData(1, 1, 1)]  
    [InlineData(100, 10, 50)]
    public void Constructor_ValidInputs_DoesNotThrow(int basePollInterval, double exponentialBackoff, int maxPollAttempts)
    {
        // Act & Assert
        var exception = Record.Exception(() => new ExponentialBackoffScheduler(basePollInterval, exponentialBackoff, maxPollAttempts));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0, 2, 15)]    
    [InlineData(-10, 2, 15)]  
    [InlineData(10, 0.99, 15)] 
    [InlineData(10, 0, 15)] 
    [InlineData(10, -1, 15)] 
    [InlineData(10, 2, 0)]    
    [InlineData(10, 2, -1)]   
    public void Constructor_InvalidInputs_ThrowsArgumentOutOfRangeException(int basePollInterval, double exponentialBackoff, int maxPollAttempts)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExponentialBackoffScheduler(basePollInterval, exponentialBackoff, maxPollAttempts)
        );
    }

    [Fact]
    public void PollIntervals_ProducesCorrectIntervals()
    {
        // Arrange
        int basePollInterval = 10;
        double exponentialBackoff = 2;
        int maxPollAttempts = 5;
        var expectedIntervals = new List<int> { 10, 20, 40, 80, 160 };

        var scheduler = new ExponentialBackoffScheduler(basePollInterval, exponentialBackoff, maxPollAttempts);

        // Act
        var actualIntervals = scheduler.PollIntervals().ToList();

        // Assert
        Assert.Equal(expectedIntervals, actualIntervals);
        Assert.Equal(maxPollAttempts, actualIntervals.Count);
    }
}

public class ConstantPollIntervalSchedulerTests
{
    [Theory]
    [InlineData(10, 15)]  
    [InlineData(1, 1)]  
    [InlineData(100, 50)]
    public void Constructor_ValidInputs_DoesNotThrow(int pollInterval, int maxPollAttempts)
    {
        // Act & Assert
        var exception = Record.Exception(() => new ConstantPollIntervalScheduler(pollInterval, maxPollAttempts));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0, 15)]    
    [InlineData(-10, 15)]
    [InlineData(10, 0)]    
    [InlineData(10, -1)]   
    public void Constructor_InvalidInputs_ThrowsArgumentOutOfRangeException(int pollInterval, int maxPollAttempts)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConstantPollIntervalScheduler(pollInterval, maxPollAttempts)
        );
    }

    [Fact]
    public void PollIntervals_ProducesCorrectIntervals()
    {
        // Arrange
        int pollInterval = 10;
        int maxPollAttempts = 5;
        var expectedIntervals = new List<int> { 10, 10, 10, 10, 10 };

        var scheduler = new ConstantPollIntervalScheduler(pollInterval, maxPollAttempts);

        // Act
        var actualIntervals = scheduler.PollIntervals().ToList();

        // Assert
        Assert.Equal(expectedIntervals, actualIntervals);
        Assert.Equal(maxPollAttempts, actualIntervals.Count);
    }
}