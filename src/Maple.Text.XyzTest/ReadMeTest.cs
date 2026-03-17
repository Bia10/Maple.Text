#pragma warning disable CA2007 // ConfigureAwait
#pragma warning disable CA1822 // Mark as static

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Maple.Text.Parsing;
using PublicApiGenerator;

namespace Maple.Text.XyzTest;

[NotInParallel]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class ReadMeTest
{
    static readonly string s_testSourceFilePath = SourceFile();

    // Navigate from src/Maple.Text.XyzTest/ up to repo root (2 levels: XyzTest → src → root)
    static readonly string s_rootDirectory =
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(s_testSourceFilePath)!, "..", ".."))
        + Path.DirectorySeparatorChar;
    static readonly string s_readmeFilePath = s_rootDirectory + "README.md";

    // ─────────────────────────────────────────────────────────────
    // SECTION 1: README example code
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void ReadMeTest_()
    {
        // Parse MapleStory markup into typed tokens
        var result = MapleTextParser.Parse("#bHello#k #t2000001# acquired");
        _ = result.Tokens;

        // Strip all markup — returns plain text
        var plain = MapleTextStripper.StripMarkup("#bHello#k");
        _ = plain;

        // Decode with stat resolution
        var decoded = MapleTextDecoder.Decode("Costs #mpCon MP");
        _ = decoded;
    }

    // ─────────────────────────────────────────────────────────────
    // SECTION 2: README sync tests — run only on net10.0
    // ─────────────────────────────────────────────────────────────

#if NET10_0
    [Test]
#endif
    public void ReadMeTest_UpdateExampleCodeInMarkdown()
    {
        var readmeLines = File.ReadAllLines(s_readmeFilePath);
        var testSourceLines = File.ReadAllLines(s_testSourceFilePath);

        var testBlocksToUpdate = new (string StartLineContains, string ReadmeLineBeforeCodeBlock)[]
        {
            (nameof(ReadMeTest_) + "()", "## Example"),
        };

        readmeLines = UpdateReadme(
            testSourceLines,
            readmeLines,
            testBlocksToUpdate,
            sourceStartLineOffset: 2,
            "    }",
            sourceEndLineOffset: 0,
            sourceWhitespaceToRemove: 8
        );

        var newReadme = string.Join(Environment.NewLine, readmeLines) + Environment.NewLine;
        File.WriteAllText(s_readmeFilePath, newReadme, System.Text.Encoding.UTF8);
    }

#if NET10_0
    [Test]
#endif
    public void ReadMeTest_PublicApi()
    {
        var publicApi = typeof(MapleTextParser).Assembly.GeneratePublicApi();
        var readmeLines = File.ReadAllLines(s_readmeFilePath);
        readmeLines = ReplaceReadmeLines(readmeLines, [publicApi], "## Public API Reference", "```csharp", 1, "```", 0);
        var newReadme = string.Join(Environment.NewLine, readmeLines) + Environment.NewLine;
        File.WriteAllText(s_readmeFilePath, newReadme, System.Text.Encoding.UTF8);
    }

    // ─────────────────────────────────────────────────────────────
    // INFRASTRUCTURE — do not modify
    // ─────────────────────────────────────────────────────────────

    static string[] UpdateReadme(
        string[] sourceLines,
        string[] readmeLines,
        (string StartLineContains, string ReadmeLineBefore)[] blocksToUpdate,
        int sourceStartLineOffset,
        string sourceEndLineStartsWith,
        int sourceEndLineOffset,
        int sourceWhitespaceToRemove,
        string readmeStartLineStartsWith = "```csharp",
        int readmeStartLineOffset = 1,
        string readmeEndLineStartsWith = "```",
        int readmeEndLineOffset = 0
    )
    {
        foreach (var (startLineContains, readmeLineBeforeBlock) in blocksToUpdate)
        {
            var sourceExampleLines = SnipLines(
                sourceLines,
                startLineContains,
                sourceStartLineOffset,
                sourceEndLineStartsWith,
                sourceEndLineOffset,
                sourceWhitespaceToRemove
            );
            readmeLines = ReplaceReadmeLines(
                readmeLines,
                sourceExampleLines,
                readmeLineBeforeBlock,
                readmeStartLineStartsWith,
                readmeStartLineOffset,
                readmeEndLineStartsWith,
                readmeEndLineOffset
            );
        }
        return readmeLines;
    }

    static string[] ReplaceReadmeLines(
        string[] readmeLines,
        string[] newLines,
        string readmeLineBeforeBlock,
        string readmeStartLineStartsWith,
        int readmeStartLineOffset,
        string readmeEndLineStartsWith,
        int readmeEndLineOffset
    )
    {
        var beforeIndex = Array.FindIndex(
            readmeLines,
            l => l.StartsWith(readmeLineBeforeBlock, StringComparison.Ordinal)
        );
        if (beforeIndex < 0)
        {
            throw new ArgumentException($"README line '{readmeLineBeforeBlock}' not found.");
        }

        var replaceStart =
            Array.FindIndex(
                readmeLines,
                beforeIndex,
                l => l.StartsWith(readmeStartLineStartsWith, StringComparison.Ordinal)
            ) + readmeStartLineOffset;
        Debug.Assert(replaceStart >= 0);
        var replaceEnd =
            Array.FindIndex(
                readmeLines,
                replaceStart,
                l => l.StartsWith(readmeEndLineStartsWith, StringComparison.Ordinal)
            ) + readmeEndLineOffset;

        return readmeLines[..replaceStart].AsEnumerable().Concat(newLines).Concat(readmeLines[replaceEnd..]).ToArray();
    }

    static string[] SnipLines(
        string[] sourceLines,
        string startLineContains,
        int startLineOffset,
        string endLineStartsWith,
        int endLineOffset,
        int whitespaceToRemove = 8
    )
    {
        var start =
            Array.FindIndex(sourceLines, l => l.Contains(startLineContains, StringComparison.Ordinal))
            + startLineOffset;
        var end =
            Array.FindIndex(sourceLines, start, l => l.StartsWith(endLineStartsWith, StringComparison.Ordinal))
            + endLineOffset;
        return sourceLines[start..end]
            .Select(l => l.Length > whitespaceToRemove ? l.Remove(0, whitespaceToRemove) : l.TrimStart())
            .ToArray();
    }

    static string SourceFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
}
