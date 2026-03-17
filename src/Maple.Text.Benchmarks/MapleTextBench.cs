using BenchmarkDotNet.Attributes;
using Maple.Text.Parsing;

namespace Maple.Text.Benchmarks;

public class MapleTextBench
{
    private const string SimpleMarkup = "#bHello World#k";
    private const string EntityMarkup = "#bItem: #t2000001# acquired#k";

    [Benchmark(Baseline = true)]
    public object Parse_SimpleMarkup() => MapleTextParser.Parse(SimpleMarkup);

    [Benchmark]
    public string StripMarkup_Simple() => MapleTextStripper.StripMarkup(SimpleMarkup);

    [Benchmark]
    public object Parse_EntityMarkup() => MapleTextParser.Parse(EntityMarkup);
}
