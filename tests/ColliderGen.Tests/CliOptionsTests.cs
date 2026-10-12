using ColliderGen.Cli;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(CliOptions))]
public class CliOptionsTests
{
    private static CliOptions Parse(params string[] args)
    {
        Assert.That(CliOptions.TryParse(args, out var options, out var error), Is.True, error);
        return options!;
    }

    private static string ParseError(params string[] args)
    {
        Assert.That(CliOptions.TryParse(args, out _, out var error), Is.False);
        return error!;
    }

    [Test]
    public void TryParse_SpriteOnly_ReturnsDefaults()
    {
        var options = Parse("sprite.png");

        Assert.That(options.InputPath, Is.EqualTo("sprite.png"));
        Assert.That(options.AllDirectory, Is.Null);
        Assert.That(options.OutputPath, Is.Null);
        Assert.That(options.DebugDraw, Is.False);
        Assert.That(options.Collider, Is.EqualTo(new ColliderOptions()));
    }

    [Test]
    public void TryParse_AllFlags_OverridesEveryDefault()
    {
        var options = Parse(
            "sprite.png", "--threshold", "64", "--epsilon", "2", "--max-vertices", "6",
            "--origin", "top-left", "--fit", "outer", "--frame-width", "36", "--output", "out.json", "--debug");

        Assert.That(options.OutputPath, Is.EqualTo("out.json"));
        Assert.That(options.DebugDraw, Is.True);
        Assert.That(options.Collider, Is.EqualTo(new ColliderOptions {
            AlphaThreshold = 64, Epsilon = 2f, MaxVertices = 6, Origin = ColliderOrigin.TopLeft, Fit = ColliderFit.Outer,
            FrameWidth = 36,
        }));
    }

    [Test]
    public void TryParse_FlagsBeforeTheSprite_AreAccepted()
    {
        var options = Parse("--debug", "--threshold", "10", "sprite.png");

        Assert.That(options.InputPath, Is.EqualTo("sprite.png"));
        Assert.That(options.Collider.AlphaThreshold, Is.EqualTo(10));
    }

    [Test]
    public void TryParse_AllDirectory_IsBatchMode()
    {
        var options = Parse("--all", "sprites", "--debug");

        Assert.That(options.AllDirectory, Is.EqualTo("sprites"));
        Assert.That(options.InputPath, Is.Null);
    }

    [Test]
    public void TryParse_FlagFollowedByAnotherFlag_Fails()
    {
        Assert.That(ParseError("sprite.png", "--origin", "--debug"), Does.Contain("--origin requires a value"));
    }

    [Test]
    public void TryParse_FlagIsLastToken_Fails()
    {
        Assert.That(ParseError("sprite.png", "--threshold"), Does.Contain("--threshold requires a value"));
    }

    [Test]
    public void TryParse_UnknownFlag_Fails()
    {
        Assert.That(ParseError("sprite.png", "--treshold", "5"), Does.Contain("Unknown option: --treshold"));
    }

    [Test]
    public void TryParse_FlagSpecifiedTwice_Fails()
    {
        Assert.That(ParseError("sprite.png", "--threshold", "10", "--threshold", "20"), Does.Contain("more than once"));
    }

    [Test]
    public void TryParse_TwoSprites_Fails()
    {
        Assert.That(ParseError("a.png", "b.png"), Does.Contain("Unexpected argument: b.png"));
    }

    [Test]
    public void TryParse_NeitherSpriteNorAll_Fails()
    {
        Assert.That(ParseError("--debug"), Does.Contain("either a sprite path or --all"));
    }

    [Test]
    public void TryParse_SpriteAndAll_Fails()
    {
        Assert.That(ParseError("sprite.png", "--all", "sprites"), Does.Contain("either a sprite path or --all"));
    }

    [Test]
    public void TryParse_OutputWithAll_Fails()
    {
        Assert.That(ParseError("--all", "sprites", "--output", "x.json"), Does.Contain("--output cannot be used with --all"));
    }

    [TestCase("--origin", "middle")]
    [TestCase("--fit", "tight")]
    [TestCase("--threshold", "256")]
    [TestCase("--threshold", "-1")]
    [TestCase("--threshold", "abc")]
    [TestCase("--epsilon", "-1")]
    [TestCase("--epsilon", "abc")]
    [TestCase("--frame-width", "0")]
    [TestCase("--max-vertices", "2")]
    [TestCase("--max-vertices", "17")]
    public void TryParse_InvalidValue_NamesTheFlag(string flag, string value)
    {
        Assert.That(ParseError("sprite.png", flag, value), Does.Contain(flag));
    }

    [TestCase("3")]
    [TestCase("16")]
    public void TryParse_MaxVerticesWithinVertexLimits_IsAccepted(string maxVertices)
    {
        Assert.That(Parse("sprite.png", "--max-vertices", maxVertices).Collider.MaxVertices, Is.EqualTo(int.Parse(maxVertices)));
    }

    [Test]
    public void TryParse_FractionalEpsilon_UsesInvariantCulture()
    {
        Assert.That(Parse("sprite.png", "--epsilon", "1.5").Collider.Epsilon, Is.EqualTo(1.5f));
    }
}
