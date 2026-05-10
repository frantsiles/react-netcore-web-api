using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions.App.Functions;

public class HeartbeatTimerFunction(ILogger<HeartbeatTimerFunction> logger)
{
    // Runs every 5 minutes — adjust NCRONTAB expression as needed
    [Function("Heartbeat")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation(
            "Heartbeat at {Time} | IsPastDue: {IsPastDue} | Next run: {NextRun}",
            DateTime.UtcNow,
            timerInfo.IsPastDue,
            timerInfo.ScheduleStatus?.Next);
    }
}
