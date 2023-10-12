using BenchmarkDotNet.Attributes;
using System;
using System.Threading;

namespace Mbc.Pcs.Net.Test.State
{
    /// <summary>
    /// Benchmark if the Timer is performant enough for PlcStateHeartBeatGenerator.
    /// </summary>
    [SimpleJob]
    public class PlcHeartBeatTimerBenchmark
    {
        private Timer _timer = new Timer(_ => { }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);

        [Benchmark]
        public void Reschedule() => _timer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }
}
