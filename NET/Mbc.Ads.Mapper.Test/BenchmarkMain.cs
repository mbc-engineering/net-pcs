using BenchmarkDotNet.Running;
using Mbc.Ads.Mapper.Test;

/* Works only without debugging in release configuration. */

BenchmarkRunner.Run<MarshallingPerformanceTest>();
