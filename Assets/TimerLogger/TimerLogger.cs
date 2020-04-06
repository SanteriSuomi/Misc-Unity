using System;
using System.Diagnostics;

/// <summary>
/// TimerLogger class is a timer that logs time elapsed and a potential message after it's dispose is called.
/// Ex. using (new TimerLogger()) { }
/// </summary>
public class TimerLogger : IDisposable
{
    private readonly Stopwatch stopWatch;
    private readonly string logMessage;

    public TimerLogger()
    {
        stopWatch = new Stopwatch();
        stopWatch.Start();
    }

    public TimerLogger(string logMessage)
    {
        stopWatch = new Stopwatch();
        stopWatch.Start();
        this.logMessage = logMessage;
    }

    public void Dispose()
    {
        stopWatch.Stop();
        if (logMessage == null)
        {
            UnityEngine.Debug.Log($"Time elapsed: {stopWatch.Elapsed.Ticks}.");
        }
        else
        {
            UnityEngine.Debug.Log($"{logMessage}, time elapsed: {stopWatch.Elapsed.Ticks}.");
        }
    }
}