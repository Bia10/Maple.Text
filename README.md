# Maple.Text

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/Maple.Text/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/Maple.Text/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/nuget/v/Maple.Text?color=purple)](https://www.nuget.org/packages/Maple.Text/)
[![License](https://img.shields.io/github/license/Bia10/Maple.Text)](https://github.com/Bia10/Maple.Text/blob/main/LICENSE)

MapleStory rich-text parsing: zero-allocation tokenizer and stripper for MapleStory custom markup used in WZ quest/NPC dialogs. Cross-platform, trimmable and AOT/NativeAOT compatible.

⭐ Please star this project if you like it. ⭐

[Example](#example) | [Public API Reference](#public-api-reference)

## Example

```csharp
// Parse MapleStory markup into typed tokens
var result = MapleTextParser.Parse("#bHello#k #t2000001# acquired");
_ = result.Tokens;

// Strip all markup — returns plain text
var plain = MapleTextStripper.StripMarkup("#bHello#k");
_ = plain;

// Decode with stat resolution
var decoded = MapleTextDecoder.Decode("Costs #mpCon MP");
_ = decoded;
```

## Public API Reference

```csharp
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/Maple.Text")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Text.Benchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Text.ComparisonBenchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Text.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Text.XyzTest")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace Maple.Text.Parsing
{
    public interface IMapleTextResolver
    {
        string ResolveBlock(string value);
        string ResolveBlockSpan(char code, System.ReadOnlySpan<char> payload);
        string ResolveClientToken(string value);
        string ResolveClientTokenSpan(char code, System.ReadOnlySpan<char> payload);
        string ResolveEntity(string value);
        string ResolveEntitySpan(char code, System.ReadOnlySpan<char> payload);
        string ResolveStat(string statName);
        string ResolveStatSpan(System.ReadOnlySpan<char> statName);
    }
    public sealed class MapleSpan : System.IEquatable<Maple.Text.Parsing.MapleSpan>
    {
        public MapleSpan(Maple.Text.Parsing.MapleSpanKind Kind, string Text, int EntityId = 0, Maple.Text.Parsing.MapleSpanColor Color = 0, bool Bold = false) { }
        public bool Bold { get; init; }
        public Maple.Text.Parsing.MapleSpanColor Color { get; init; }
        public int EntityId { get; init; }
        public Maple.Text.Parsing.MapleSpanKind Kind { get; init; }
        public string Text { get; init; }
    }
    public enum MapleSpanColor : byte
    {
        Default = 0,
        Blue = 1,
        Red = 2,
        Cyan = 3,
        Dark = 4,
        Gray = 5,
        Sky = 6,
        Black = 7,
    }
    public enum MapleSpanKind : byte
    {
        Text = 0,
        LineBreak = 1,
        ItemLink = 2,
        MobLink = 3,
        MapLink = 4,
        NpcLink = 5,
        SkillLink = 6,
        Icon = 7,
    }
    public static class MapleText
    {
        public static string Bold(string text) { }
        public static Maple.Text.Parsing.MapleTextBuilder Builder() { }
        public static string Colorize(Maple.Text.Parsing.MapleTextColor color, string text) { }
        public static bool ContainsMarkup(System.ReadOnlySpan<char> text) { }
        public static bool ContainsMarkup(string text) { }
        public static string Decode(string text, Maple.Text.Parsing.IMapleTextResolver? resolver = null) { }
        public static string InsertLink(Maple.Text.Parsing.MapleTextLinkType linkType, long templateId, int josaSuffix = 0) { }
        public static string InvokeMenuDialog(string question, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>> entries, Maple.Text.Parsing.MapleTextColor color = 1) { }
        public static string InvokeOkDialog(string message) { }
        public static string InvokeSelectDialog(string question, [System.Runtime.CompilerServices.ParamCollection] [System.Runtime.CompilerServices.ScopedRef] System.ReadOnlySpan<string> options) { }
        public static string InvokeYesNoDialog(string question, string yesText = "Yes", string noText = "No") { }
        public static Maple.Text.Parsing.MapleTextParseResult Parse(string text) { }
        public static string StripMarkup(System.ReadOnlySpan<char> text) { }
        public static string StripMarkup(string text) { }
        public static string Stylize(Maple.Text.Parsing.MapleTextStyle style, string text) { }
    }
    public sealed class MapleTextBuilder : System.IDisposable
    {
        public MapleTextBuilder() { }
        public Maple.Text.Parsing.MapleTextBuilder Append(System.ReadOnlySpan<char> text) { }
        public Maple.Text.Parsing.MapleTextBuilder Append(string text) { }
        public Maple.Text.Parsing.MapleTextBuilder Bold(System.ReadOnlySpan<char> content) { }
        public Maple.Text.Parsing.MapleTextBuilder Bold(string content) { }
        public string Build() { }
        public Maple.Text.Parsing.MapleTextBuilder CanvasLoad(System.ReadOnlySpan<char> path, bool outline = false) { }
        public Maple.Text.Parsing.MapleTextBuilder CanvasLoad(string path, bool outline = false) { }
        public Maple.Text.Parsing.MapleTextBuilder CharacterName(int josa = 0) { }
        public Maple.Text.Parsing.MapleTextBuilder Color(Maple.Text.Parsing.MapleTextColor color, System.ReadOnlySpan<char> content) { }
        public Maple.Text.Parsing.MapleTextBuilder Color(Maple.Text.Parsing.MapleTextColor color, string content) { }
        public void Dispose() { }
        public Maple.Text.Parsing.MapleTextBuilder Gauge(System.ReadOnlySpan<char> path) { }
        public Maple.Text.Parsing.MapleTextBuilder Gauge(string path) { }
        public Maple.Text.Parsing.MapleTextBuilder ItemIcon(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder ItemIconSlot(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder ItemName(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder ItemNameAlt(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder LabeledNpcString(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder ListEntry(int index, System.ReadOnlySpan<char> text, Maple.Text.Parsing.MapleTextColor color = 0) { }
        public Maple.Text.Parsing.MapleTextBuilder ListEntry(int index, string text, Maple.Text.Parsing.MapleTextColor color = 0) { }
        public Maple.Text.Parsing.MapleTextBuilder LiteralHash() { }
        public Maple.Text.Parsing.MapleTextBuilder MapName(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder MobName(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder NewLine() { }
        public Maple.Text.Parsing.MapleTextBuilder NpcName(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder OpenColor(Maple.Text.Parsing.MapleTextColor color) { }
        public Maple.Text.Parsing.MapleTextBuilder OpenStyle(Maple.Text.Parsing.MapleTextStyle style) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestGauge(System.ReadOnlySpan<char> key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestGauge(string key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestMobCount() { }
        public Maple.Text.Parsing.MapleTextBuilder QuestMobName(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestPlaytime(System.ReadOnlySpan<char> key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestPlaytime(string key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestRecord(System.ReadOnlySpan<char> key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestRecord(string key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestState() { }
        public Maple.Text.Parsing.MapleTextBuilder QuestSummaryIcon(System.ReadOnlySpan<char> name) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestSummaryIcon(string name) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestTimer(System.ReadOnlySpan<char> key) { }
        public Maple.Text.Parsing.MapleTextBuilder QuestTimer(string key) { }
        public Maple.Text.Parsing.MapleTextBuilder ResetAll() { }
        public Maple.Text.Parsing.MapleTextBuilder ResetStyle() { }
        public Maple.Text.Parsing.MapleTextBuilder RewardToggle() { }
        public Maple.Text.Parsing.MapleTextBuilder SkillRef(long id) { }
        public Maple.Text.Parsing.MapleTextBuilder Stat(System.ReadOnlySpan<char> statName) { }
        public Maple.Text.Parsing.MapleTextBuilder Stat(string statName) { }
        public Maple.Text.Parsing.MapleTextBuilder Style(Maple.Text.Parsing.MapleTextStyle style, System.ReadOnlySpan<char> content) { }
        public Maple.Text.Parsing.MapleTextBuilder Style(Maple.Text.Parsing.MapleTextStyle style, string content) { }
        public override string ToString() { }
    }
    public enum MapleTextColor : byte
    {
        None = 0,
        Blue = 1,
        Red = 2,
        Cyan = 3,
        Black = 4,
        Dark = 5,
        Gray = 6,
        Sky = 7,
    }
    public static class MapleTextDecoder
    {
        public static string Decode(string text, Maple.Text.Parsing.IMapleTextResolver? resolver = null) { }
    }
    public enum MapleTextLinkType : byte
    {
        ItemName = 0,
        ItemNameAlt = 1,
        ItemIcon = 2,
        ItemIconSlot = 3,
        MobName = 4,
        MapName = 5,
        NpcName = 6,
        CharacterName = 7,
        SkillName = 8,
        QuestMobName = 9,
        LabeledNpcString = 10,
    }
    public readonly struct MapleTextParseResult
    {
        public bool HasErrors { get; }
        public System.Collections.Generic.IReadOnlyList<Maple.Text.Parsing.MapleTextToken> Tokens { get; }
        public System.ReadOnlySpan<Maple.Text.Parsing.MapleTextToken> TokensSpan { get; }
    }
    public static class MapleTextParser
    {
        public static Maple.Text.Parsing.MapleTextParseResult Parse(string text) { }
    }
    public static class MapleTextStripper
    {
        public static bool ContainsMarkup(System.ReadOnlySpan<char> text) { }
        public static bool ContainsMarkup(string text) { }
        public static string StripMarkup(System.ReadOnlySpan<char> text) { }
        public static string StripMarkup(string text) { }
    }
    public enum MapleTextStyle : byte
    {
        None = 0,
        Bold = 1,
        Normal = 2,
        Small = 3,
    }
    public readonly struct MapleTextToken
    {
        public char Code { get; init; }
        public Maple.Text.Parsing.MapleTextTokenKind Kind { get; init; }
        public ushort Length { get; init; }
        public ushort PayloadLength { get; init; }
        public ushort PayloadStart { get; init; }
        public ushort Start { get; init; }
        public System.ReadOnlySpan<char> GetPayload(System.ReadOnlySpan<char> source) { }
        public System.ReadOnlySpan<char> GetRaw(System.ReadOnlySpan<char> source) { }
        public string GetRawString(string source) { }
        public string GetValue(string source) { }
    }
    public enum MapleTextTokenKind : byte
    {
        Text = 0,
        StyleCode = 1,
        StatToken = 2,
        EntityReference = 3,
        Escape = 4,
        Block = 5,
        ClientToken = 6,
        UnknownCode = 7,
        UnterminatedEntity = 8,
        UnterminatedBlock = 9,
    }
}
```
