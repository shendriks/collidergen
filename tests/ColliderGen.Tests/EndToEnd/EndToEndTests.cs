using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ColliderGen.Cli;
using NUnit.Framework;
using StbImageSharp;

namespace ColliderGen.Tests.EndToEnd;

[TestFixture]
[TestOf(typeof(ColliderGenCli))]
public class EndToEndTests
{
    private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "EndToEnd");

    private string _directory = string.Empty;

    private static IEnumerable<TestCaseData> Cases()
    {
        yield return Case("single-pixel");
        yield return Case("fully-transparent");
        yield return Case("alpha-threshold-boundary");
        yield return Case("alpha-threshold-boundary", "threshold-127", "--threshold", "127");
        yield return Case("alpha-threshold-boundary", "threshold-255", "--threshold", "255");
        yield return Case("thin-diagonal-line");
        yield return Case("two-islands");
        yield return Case("diagonal-joint-v");
        yield return Case("diagonal-joint-blocks");
        yield return Case("ring-with-hole");
        yield return Case("circle");
        yield return Case("circle", "max-vertices-6", "--max-vertices", "6");
        yield return Case("circle", "fit-outer", "--fit", "outer", "--max-vertices", "6");
        yield return Case("strip-with-empty-frame");
        yield return Case("strip-with-empty-frame", "top-left", "--origin", "top-left");
        yield return Case("explicit-frame-width", null, "--frame-width", "10");
    }

    private static TestCaseData Case(string sprite, string? variant = null, params string[] arguments)
    {
        var caseName = variant is null ? sprite : $"{sprite}.{variant}";
        return new TestCaseData(sprite, caseName, arguments).SetName(caseName);
    }

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "collidergen-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    [TestCaseSource(nameof(Cases))]
    public void Given_Sprite_When_RunWithDebug_Then_JsonAndOverlayMatchExpected(string sprite, string caseName, string[] arguments)
    {
        var spritePath = Path.Combine(_directory, sprite + ".png");
        File.Copy(Path.Combine(TestDataDirectory, sprite + ".png"), spritePath);

        var exitCode = new ColliderGenCli(TextWriter.Null, TextWriter.Null, new DiskFileSystem()).Run([spritePath, "--debug", .. arguments]);

        Assert.That(exitCode, Is.Zero);
        AssertJsonMatches(Path.ChangeExtension(spritePath, ".collider.json"), ExpectedPath(caseName, ".expected.json"));
        AssertOverlayMatches(Path.Combine(_directory, sprite + ".collider-debug.png"), ExpectedPath(caseName, ".expected-debug.png"));
    }

    private static string ExpectedPath(string caseName, string suffix) => Path.Combine(TestDataDirectory, caseName + suffix);

    private static void AssertJsonMatches(string actualPath, string expectedPath)
    {
        var actual = Deserialize(actualPath);
        var expected = Deserialize(expectedPath);

        Assert.That(actual.Frames, Has.Length.EqualTo(expected.Frames.Length));
        for (var frame = 0; frame < expected.Frames.Length; frame++) {
            Assert.That(actual.Frames[frame].Vertices, Is.EqualTo(expected.Frames[frame].Vertices), $"frame {frame}");
        }
    }

    private static ColliderDto Deserialize(string path) => JsonSerializer.Deserialize<ColliderDto>(File.ReadAllText(path))!;

    private static void AssertOverlayMatches(string actualPath, string expectedPath)
    {
        var actual = Decode(actualPath);
        var expected = Decode(expectedPath);

        Assert.That((actual.Width, actual.Height), Is.EqualTo((expected.Width, expected.Height)));
        Assert.That(actual.Data, Is.EqualTo(expected.Data));
    }

    private static ImageResult Decode(string path)
    {
        using var stream = File.OpenRead(path);
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }
}
