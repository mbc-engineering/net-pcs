using BenchmarkDotNet.Running;
using Mbc.Ads.Mapper;
using Mbc.Pcs.Net.State;

/* Works only without debugging in release configuration. */

BenchmarkRunner.Run<PlcHeartBeatTimerBenchmark>();
BenchmarkRunner.Run<MarshallingPerformanceTest>();
