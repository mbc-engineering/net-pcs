using BenchmarkDotNet.Running;
using Mbc.Pcs.Net.Test.State;

/* Works only without debugging in release configuration. */

BenchmarkRunner.Run<PlcHeartBeatTimerBenchmark>();
