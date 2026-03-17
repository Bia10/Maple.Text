using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Maple.Text.Parsing;

namespace Maple.Text.ComparisonBenchmarks;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[BenchmarkCategory("0")]
public class TestBench
{
    [Params(25_000)]
    public int Count { get; set; }

    [Benchmark(Baseline = true)]
    public object MapleText______() => MapleTextParser.Parse("#bHello #t2000001# World#k");
}
