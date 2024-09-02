namespace Sugarmaple.TheSeed.Api;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

internal class TaskDelayProcessor
{
    private readonly Thread _thread;
    private readonly Stopwatch _stopwatch = new();
    private readonly BlockingCollection<Task> _queue = new();

    public TaskDelayProcessor()
    {
        _thread = new(Operate);
        _thread.Start();
    }

    private void Operate()
    {
        while (true)
        {
            _stopwatch.Restart();
            var task = _queue.Take();
            task.Start();
            task.Wait();
            _stopwatch.Stop();

            var sleepTime = TimeSpan.FromSeconds(1) - _stopwatch.Elapsed;
            if (sleepTime > TimeSpan.Zero)
                Thread.Sleep(sleepTime);
        }
    }

    internal Task<T> Enqueue<T>(Task<T> task)
    {
        var queueTask = task.ContinueWith(t => t.Result);
        _queue.Add(task);
        return queueTask;
    }
}