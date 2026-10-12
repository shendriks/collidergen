using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ColliderGen.Cli;
using NUnit.Framework;
using StbImageWriteSharp;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(ColliderGenCli))]
public class ColliderGenCliTests
{
    private static readonly string SpriteDirectory = Path.Combine("sprites", "ships");

    private InMemoryFileSystem _files = new();
    private string _output = string.Empty;
    private string _error = string.Empty;

    [SetUp]
    public void SetUp() => _files = new InMemoryFileSystem();

    private int Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = new ColliderGenCli(output, error, _files).Run(args);

        _output = output.ToString();
        _error = error.ToString();
        return exitCode;
    }

    private string WriteSprite(string name, int frameSize = 6, int frameCount = 1)
    {
        var width = frameSize * frameCount;
        var rgba = new byte[width * frameSize * 4];
        for (var i = 0; i < rgba.Length; i++) {
            rgba[i] = 255;
        }

        var path = Path.Combine(SpriteDirectory, name);
        using var stream = new MemoryStream();
        new ImageWriter().WritePng(rgba, width, frameSize, ColorComponents.RedGreenBlueAlpha, stream);
        _files.Write(path, stream.ToArray());
        return path;
    }

    private ColliderDto ReadCollider(string path) => JsonSerializer.Deserialize<ColliderDto>(_files.Read(path))!;

    [Test]
    public void Run_NoArguments_PrintsUsageAndFails()
    {
        var exitCode = Run();

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("Usage: collidergen"));
    }

    [Test]
    public void Run_InvalidOption_PrintsTheProblemAndUsageAndFails()
    {
        var exitCode = Run("sprite.png", "--origin", "middle");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("--origin must be"));
        Assert.That(_error, Does.Contain("Usage: collidergen"));
    }

    [Test]
    public void Run_AllOnAMissingDirectory_Fails()
    {
        var exitCode = Run("--all", Path.Combine(SpriteDirectory, "nope"));

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("Directory not found"));
    }

    [Test]
    public void Run_MissingSprite_ReportsTheFileAndFails()
    {
        var spritePath = Path.Combine(SpriteDirectory, "missing.png");

        var exitCode = Run(spritePath);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("missing.png"));
    }

    [Test]
    public void Run_FileThatIsNotAPng_ReportsTheFileAndFails()
    {
        var spritePath = Path.Combine(SpriteDirectory, "broken.png");
        _files.Write(spritePath, "not an image"u8.ToArray());

        var exitCode = Run(spritePath);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("broken.png"));
    }

    [Test]
    public void Run_FrameWidthNotDividingTheImage_ReportsTheFileAndFails()
    {
        var spritePath = WriteSprite("ship.png", frameSize: 6);

        var exitCode = Run(spritePath, "--frame-width", "4");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_error, Does.Contain("ship.png"));
        Assert.That(_files.Exists(Path.ChangeExtension(spritePath, ".collider.json")), Is.False);
    }

    [Test]
    public void Run_All_ContinuesPastABrokenSpriteAndFails()
    {
        WriteSprite("good.png");
        _files.Write(Path.Combine(SpriteDirectory, "broken.png"), "not an image"u8.ToArray());

        var exitCode = Run("--all", SpriteDirectory);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(_files.Exists(Path.Combine(SpriteDirectory, "good.collider.json")), Is.True);
        Assert.That(_error, Does.Contain("broken.png"));
        Assert.That(_output, Does.Contain("Processed 1 sprites"));
    }

    [Test]
    public void Run_SingleSprite_WritesOneHullPerFrame()
    {
        var spritePath = WriteSprite("ship.png", frameSize: 6, frameCount: 3);

        var exitCode = Run(spritePath);

        Assert.That(exitCode, Is.Zero);
        var collider = ReadCollider(Path.ChangeExtension(spritePath, ".collider.json"));
        Assert.That(collider.Frames, Has.Length.EqualTo(3));
        Assert.That(collider.Frames, Has.All.Property(nameof(FrameDto.Vertices)).Length.EqualTo(4));
    }

    [Test]
    public void Run_SingleSprite_HonorsTheOutputOption()
    {
        var spritePath = WriteSprite("ship.png");
        var outputPath = Path.Combine(SpriteDirectory, "custom.json");

        var exitCode = Run(spritePath, "--output", outputPath);

        Assert.That(exitCode, Is.Zero);
        Assert.That(_files.Exists(outputPath), Is.True);
        Assert.That(_files.Exists(Path.ChangeExtension(spritePath, ".collider.json")), Is.False);
    }

    [Test]
    public void Run_CenterOrigin_PlacesTheHullAroundTheFrameCenter()
    {
        var spritePath = WriteSprite("ship.png", frameSize: 6);

        var exitCode = Run(spritePath);

        Assert.That(exitCode, Is.Zero);
        var vertices = ReadCollider(Path.ChangeExtension(spritePath, ".collider.json")).Frames[0].Vertices;
        Assert.That(vertices.Min(vertex => vertex.X), Is.EqualTo(-3f));
        Assert.That(vertices.Max(vertex => vertex.Y), Is.EqualTo(3f));
    }

    [Test]
    public void Run_TopLeftOrigin_PlacesTheHullAtTheFrameCorner()
    {
        var spritePath = WriteSprite("ship.png", frameSize: 6);

        var exitCode = Run(spritePath, "--origin", "top-left");

        Assert.That(exitCode, Is.Zero);
        var vertices = ReadCollider(Path.ChangeExtension(spritePath, ".collider.json")).Frames[0].Vertices;
        Assert.That(vertices.Min(vertex => vertex.X), Is.Zero);
        Assert.That(vertices.Max(vertex => vertex.Y), Is.EqualTo(6f));
    }

    [Test]
    public void Run_Debug_WritesTheOverlayPng()
    {
        var spritePath = WriteSprite("ship.png");

        var exitCode = Run(spritePath, "--debug");

        Assert.That(exitCode, Is.Zero);
        Assert.That(_files.Exists(Path.ChangeExtension(spritePath, ".collider-debug.png")), Is.True);
        Assert.That(_output, Does.Contain("Wrote debug overlay"));
    }

    [Test]
    public void Run_All_ProcessesEverySpriteAndSkipsDebugOverlays()
    {
        WriteSprite("one.png");
        WriteSprite("two.png");
        var overlayPath = WriteSprite("three.collider-debug.png");

        var exitCode = Run("--all", SpriteDirectory);

        Assert.That(exitCode, Is.Zero);
        Assert.That(_files.EnumerateFilesRecursively(SpriteDirectory).Count(path => path.EndsWith(".collider.json", StringComparison.Ordinal)), Is.EqualTo(2));
        Assert.That(_files.Exists(Path.ChangeExtension(overlayPath, ".collider.json")), Is.False);
        Assert.That(_output, Does.Contain("Processed 2 sprites"));
    }
}
