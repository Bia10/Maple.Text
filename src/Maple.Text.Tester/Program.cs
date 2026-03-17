using Maple.Text.Parsing;

Console.WriteLine($"Maple.Text version: {typeof(MapleTextParser).Assembly.GetName().Version}");

var result = MapleTextParser.Parse("#bHello#k #t2000001#");
Console.WriteLine($"Tokens: {result.Tokens.Count}, HasErrors: {result.HasErrors}");
Console.WriteLine("OK");
